using System.Globalization;
using System.Text;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Pdf;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Composes standalone HTML for the Jira report.
/// </summary>
public sealed class HtmlContentComposer : IHtmlContentComposer
{
    /// <inheritdoc />
    public string Compose(JiraPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var content = new StringBuilder(32 * 1024);
        _ = content.Append(BuildRatiosSection(reportData));
        _ = content.Append(BuildIssueTimelineTable(
            "done-issues",
            "Issues moved to Done in selected period",
            reportData.DoneIssues,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData));
        _ = content.Append(BuildDuration75PerTypeTable(
            "done-duration-75",
            $"{PdfPresentationFormatting.GetWorkDuration75Title(reportData.Settings.ShowTimeCalculationsInHoursOnly)} per type",
            reportData.DoneDaysAtWork75PerType,
            reportData.Settings.ShowTimeCalculationsInHoursOnly));

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            _ = content.Append(BuildIssueTimelineTable(
                "rejected-issues",
                "Issues moved to Rejected in selected period",
                reportData.RejectedIssues,
                rejectStatusName,
                "Rejected At",
                reportData));
        }

        _ = content.Append(BuildPathSummaryTable(reportData.PathSummary));
        _ = content.Append(BuildPathGroupsTable(reportData));
        _ = content.Append(BuildReleaseTable(reportData));
        _ = content.Append(BuildArchTasksTable(reportData));
        _ = content.Append(BuildGlobalIncidentsTable(reportData));
        _ = content.Append(BuildFailuresTable(reportData));

        return ApplyTemplate(
            HtmlTemplateLoader.LoadReportTemplate(),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__PROJECT__"] = HtmlPresentationHelpers.Encode(reportData.Settings.ProjectKey.Value),
                ["__GENERATED_AT__"] = HtmlPresentationHelpers.Encode(
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                ["__PERIOD__"] = HtmlPresentationHelpers.Encode(reportData.Settings.ReportPeriod.Label),
                ["__DONE_STATUS__"] = HtmlPresentationHelpers.Encode(reportData.Settings.DoneStatusName.Value),
                ["__SEARCH_ISSUES__"] = reportData.SearchIssueCount.Value.ToString(CultureInfo.InvariantCulture),
                ["__DONE_ISSUES__"] = reportData.DoneIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__REJECTED_ISSUES__"] = reportData.RejectedIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__PATH_GROUPS__"] = reportData.PathSummary.PathGroupCount.Value.ToString(CultureInfo.InvariantCulture),
                ["__FAILED_ISSUES__"] = reportData.Failures.Count.ToString(CultureInfo.InvariantCulture),
                ["__CONTENT__"] = content.ToString()
            });
    }

    private static string BuildRatiosSection(JiraPdfReportData reportData)
    {
        var rows = new List<TableRow>();
        AddRatioRows(rows, "All tasks", reportData.AllTasksCreatedThisMonth, reportData.AllTasksOpenThisMonth, reportData.AllTasksMovedToDoneThisMonth, reportData.AllTasksRejectedThisMonth, reportData.AllTasksFinishedThisMonth);
        AddRatioRows(rows, "Bugs", reportData.BugCreatedThisMonth, new ItemCount(reportData.BugOpenIssues.Count), reportData.BugMovedToDoneThisMonth, reportData.BugRejectedThisMonth, reportData.BugFinishedThisMonth);

        return BuildTableSection(
            "ratios",
            "Task Ratios",
            "No ratio data available.",
            MetricColumns,
            rows,
            defaultSortColumn: 0,
            compact: true);
    }

    private static void AddRatioRows(
        List<TableRow> rows,
        string scope,
        ItemCount? created,
        ItemCount? open,
        ItemCount? done,
        ItemCount? rejected,
        ItemCount? finished)
    {
        if (!created.HasValue || !open.HasValue || !done.HasValue || !rejected.HasValue || !finished.HasValue)
        {
            return;
        }

        rows.Add(BuildMetricRow($"{scope}: Created", created.Value.Value));
        rows.Add(BuildMetricRow($"{scope}: Open", open.Value.Value));
        rows.Add(BuildMetricRow($"{scope}: Done", done.Value.Value));
        rows.Add(BuildMetricRow($"{scope}: Rejected", rejected.Value.Value));
        rows.Add(BuildMetricRow($"{scope}: Finished", finished.Value.Value));
        rows.Add(new TableRow(
        [
            BuildTextCell($"{scope}: Finished / Created"),
            BuildTextCell(PdfPresentationFormatting.BuildFinishedToCreatedRatioText(created.Value, finished.Value))
        ]));
    }

    private static string BuildIssueTimelineTable(
        string sectionId,
        string title,
        IReadOnlyList<IssueTimeline> issues,
        StatusName targetStatusName,
        string atColumnTitle,
        JiraPdfReportData reportData)
    {
        var columns = new[]
        {
            new TableColumn("#", "number", "#", "narrow"),
            new TableColumn("Issue", "text", "Issue", "issue-column"),
            new TableColumn("Type", "text", "Type"),
            new TableColumn("Sub-items", "number", "Sub-items"),
            new TableColumn("Code", "text", "Code"),
            new TableColumn("Summary", "text", "Summary", "summary-column"),
            new TableColumn("Created At", "number", "Created At"),
            new TableColumn(atColumnTitle, "number", atColumnTitle),
            new TableColumn(PdfPresentationFormatting.GetWorkDurationColumnLabel(reportData.Settings.ShowTimeCalculationsInHoursOnly), "number", "Duration")
        };

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rows = new List<TableRow>(orderedIssues.Length);

        for (var i = 0; i < orderedIssues.Length; i++)
        {
            var issue = orderedIssues[i];
            var issueUrl = HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key);
            var lastStatusAt = issue.TryGetLastReachedAt(targetStatusName);
            var workDuration = issue.TryBuildWorkDuration(targetStatusName);
            rows.Add(new TableRow(
            [
                BuildTextCell(HtmlPresentationHelpers.FormatCount(i + 1), i + 1),
                BuildLinkCell(issue.Key.Value, issueUrl),
                BuildTextCell(issue.IssueType.Value),
                BuildTextCell(HtmlPresentationHelpers.FormatCount(issue.SubItemsCount), issue.SubItemsCount),
                BuildTextCell(issue.HasPullRequest ? "+" : string.Empty),
                BuildTextCell(issue.Summary.Value),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(issue.Created), issue.Created.ToUnixTimeSeconds()),
                BuildTextCell(PdfPresentationFormatting.BuildLastStatusAtText(issue, targetStatusName), lastStatusAt?.ToUnixTimeSeconds()),
                BuildTextCell(
                    PdfPresentationFormatting.BuildWorkDurationText(issue, targetStatusName, reportData.Settings.ShowTimeCalculationsInHoursOnly),
                    workDuration?.TotalMinutes)
            ]));
        }

        return BuildTableSection(sectionId, title, "No issues.", columns, rows, defaultSortColumn: 1);
    }

    private static string BuildDuration75PerTypeTable(
        string sectionId,
        string title,
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        bool showTimeCalculationsInHoursOnly)
    {
        var rows = summaries
            .OrderByDescending(static item => item.DaysAtWorkP75)
            .ThenByDescending(static item => item.IssueCount.Value)
            .ThenBy(static item => item.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(summary => new TableRow(
            [
                BuildTextCell(summary.IssueType.Value),
                BuildTextCell(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture), summary.IssueCount.Value),
                BuildTextCell(
                    PdfPresentationFormatting.FormatWorkDurationValue(summary.DaysAtWorkP75, showTimeCalculationsInHoursOnly),
                    summary.DaysAtWorkP75.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No data.",
            [
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn(PdfPresentationFormatting.GetWorkDuration75Title(showTimeCalculationsInHoursOnly), "number", "75P")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

    private static string BuildPathSummaryTable(PathGroupsSummary summary) =>
        BuildTableSection(
            "path-summary",
            "Path Groups Summary",
            "No path summary available.",
            MetricColumns,
            [
                BuildMetricRow("Successful", summary.SuccessfulCount.Value),
                BuildMetricRow("Matched stage", summary.MatchedStageCount.Value),
                BuildMetricRow("Failed", summary.FailedCount.Value),
                BuildMetricRow("Path groups", summary.PathGroupCount.Value)
            ],
            defaultSortColumn: 0,
            compact: true);

    private static string BuildPathGroupsTable(JiraPdfReportData reportData)
    {
        var rows = reportData.PathGroups
            .Select((group, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildTextCell(group.Issues.Count.ToString(CultureInfo.InvariantCulture), group.Issues.Count),
                BuildTextCell(group.PathLabel.Value),
                BuildTextCell(PdfPresentationHelpers.ToDurationLabel(group.TotalP75, reportData.Settings.ShowTimeCalculationsInHoursOnly), group.TotalP75.TotalMinutes),
                BuildTextCell(string.Join(", ", group.Issues.Select(static issue => issue.Key.Value)))
            ]))
            .ToList();

        return BuildTableSection(
            "path-groups",
            "Path Groups",
            "No path groups.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn("Path", "text", "Path", "summary-column"),
                new TableColumn("TTM 75P", "number", "TTM 75P"),
                new TableColumn("Issue Keys", "text", "Issue Keys")
            ],
            rows,
            defaultSortColumn: 1,
            defaultSortDirection: "desc");
    }

    private static string BuildReleaseTable(JiraPdfReportData reportData)
    {
        if (reportData.Settings.ReleaseReport is null)
        {
            return string.Empty;
        }

        var rows = reportData.ReleaseIssues
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((release, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildTextCell(HtmlPresentationHelpers.FormatDate(release.ReleaseDate), release.ReleaseDate.ToDateTime(TimeOnly.MinValue).Ticks),
                BuildLinkCell(release.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, release.Key)),
                BuildTextCell(release.Status.Value),
                BuildTextCell(release.Tasks == 0 ? "-" : release.Tasks.ToString(CultureInfo.InvariantCulture), release.Tasks),
                BuildTextCell(string.Join(", ", release.EnvironmentNames)),
                BuildTextCell(string.IsNullOrWhiteSpace(release.RollbackType) ? "-" : release.RollbackType),
                BuildTextCell(release.Title.Value)
            ], release.IsHotFix ? "warning-row" : null))
            .ToList();

        return BuildTableSection(
            "releases",
            "Release Report",
            "No releases found for selected period.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Release Date", "number", "Release Date"),
                new TableColumn("Jira ID", "text", "Jira ID", "issue-column"),
                new TableColumn("Status", "text", "Status"),
                new TableColumn("Tasks", "number", "Tasks"),
                new TableColumn("Environments", "text", "Environments"),
                new TableColumn("Rollback", "text", "Rollback"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 1);
    }

    private static string BuildArchTasksTable(JiraPdfReportData reportData)
    {
        if (reportData.Settings.ArchTasksReport is null)
        {
            return string.Empty;
        }

        var rows = reportData.ArchTasks
            .OrderBy(static task => task.CreatedAt)
            .ThenBy(static task => task.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((task, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(task.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, task.Key)),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(task.CreatedAt), task.CreatedAt.ToUnixTimeSeconds()),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(task.ResolvedAt), task.ResolvedAt?.ToUnixTimeSeconds()),
                BuildTextCell(task.Title.Value)
            ]))
            .ToList();

        return BuildTableSection(
            "arch-tasks",
            "Architecture Tasks",
            "No architecture tasks found.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Created", "number", "Created"),
                new TableColumn("Resolved", "number", "Resolved"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 2);
    }

    private static string BuildGlobalIncidentsTable(JiraPdfReportData reportData)
    {
        if (reportData.Settings.GlobalIncidentsReport is null)
        {
            return string.Empty;
        }

        var rows = reportData.GlobalIncidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((incident, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(incident.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, incident.Key)),
                BuildTextCell(PdfPresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentStartUtc), incident.IncidentStartUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PdfPresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc), incident.IncidentRecoveryUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PdfPresentationFormatting.FormatIncidentDuration(incident.Duration, reportData.Settings.ShowTimeCalculationsInHoursOnly), incident.Duration?.TotalMinutes),
                BuildTextCell(incident.Impact ?? "-"),
                BuildTextCell(incident.Urgency ?? "-"),
                BuildTextCell(incident.Title.Value)
            ]))
            .ToList();

        return BuildTableSection(
            "global-incidents",
            "Global Incidents",
            "No incidents found.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Started UTC", "number", "Started UTC"),
                new TableColumn("Recovered UTC", "number", "Recovered UTC"),
                new TableColumn("Duration", "number", "Duration"),
                new TableColumn("Impact", "text", "Impact"),
                new TableColumn("Urgency", "text", "Urgency"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 2);
    }

    private static string BuildFailuresTable(JiraPdfReportData reportData)
    {
        var rows = reportData.Failures
            .Select((failure, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(failure.IssueKey.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, failure.IssueKey)),
                BuildTextCell(failure.Reason.Value)
            ]))
            .ToList();

        return BuildTableSection(
            "failures",
            "Failed Issues",
            "No failed issue loads.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Reason", "text", "Reason", "summary-column")
            ],
            rows,
            defaultSortColumn: 0);
    }

    private static string BuildTableSection(
        string sectionId,
        string title,
        string emptyMessage,
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        int? defaultSortColumn,
        string defaultSortDirection = "asc",
        bool compact = false)
    {
        var containerClass = compact ? "table-section compact-section" : "table-section";
        var html = new StringBuilder();
        _ = html.AppendLine(string.Concat("<section class=\"", containerClass, "\" id=\"", HtmlPresentationHelpers.EncodeAttribute(sectionId), "\">"));
        _ = html.AppendLine(string.Concat("  <div class=\"section-header\"><h2>", HtmlPresentationHelpers.Encode(title), "</h2></div>"));
        _ = html.AppendLine("  <div class=\"table-panel\" data-table-panel>");
        _ = html.AppendLine("    <div class=\"table-controls\">");
        _ = html.AppendLine("      <input class=\"search\" data-table-search type=\"search\" placeholder=\"Search this table\">");
        _ = html.AppendLine("      <button class=\"button\" data-table-reset type=\"button\">Reset Filters</button>");
        _ = html.AppendLine("    </div>");
        _ = html.AppendLine("    <div class=\"table-wrap\"><div class=\"scroll\">");
        _ = html.AppendLine(string.Concat(
            "      <table class=\"report-table\" data-default-sort-column=\"",
            defaultSortColumn.HasValue ? defaultSortColumn.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
            "\" data-default-sort-direction=\"",
            HtmlPresentationHelpers.EncodeAttribute(defaultSortDirection),
            "\">"));
        _ = html.AppendLine("        <thead><tr>");

        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var thClass = string.IsNullOrWhiteSpace(column.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(column.CssClass)}\"";
            _ = html.AppendLine(string.Concat(
                "          <th",
                thClass,
                "><button class=\"th-button\" data-sort-column=\"",
                columnIndex.ToString(CultureInfo.InvariantCulture),
                "\" data-sort-type=\"",
                HtmlPresentationHelpers.EncodeAttribute(column.SortType),
                "\" type=\"button\"><span>",
                HtmlPresentationHelpers.Encode(column.Header),
                "</span><span class=\"sort-indicator\"></span></button></th>"));
        }

        _ = html.AppendLine("        </tr><tr class=\"filters\">");
        foreach (var column in columns)
        {
            var thClass = string.IsNullOrWhiteSpace(column.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(column.CssClass)}\"";
            _ = html.AppendLine(string.Concat(
                "          <th",
                thClass,
                "><input class=\"filter-input\" data-filter-column placeholder=\"",
                HtmlPresentationHelpers.EncodeAttribute(column.FilterPlaceholder),
                "\" type=\"search\"></th>"));
        }

        _ = html.AppendLine("        </tr></thead><tbody>");
        if (rows.Count == 0)
        {
            _ = html.AppendLine(string.Concat(
                "          <tr class=\"empty\"><td class=\"empty-cell\" colspan=\"",
                columns.Count.ToString(CultureInfo.InvariantCulture),
                "\">",
                HtmlPresentationHelpers.Encode(emptyMessage),
                "</td></tr>"));
        }
        else
        {
            foreach (var row in rows)
            {
                var rowClass = string.IsNullOrWhiteSpace(row.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(row.CssClass)}\"";
                _ = html.AppendLine(string.Concat("          <tr", rowClass, ">"));
                foreach (var cell in row.Cells)
                {
                    _ = html.AppendLine(string.Concat(
                        "            <td data-sort='",
                        HtmlPresentationHelpers.EncodeAttribute(cell.SortValue),
                        "' data-filter='",
                        HtmlPresentationHelpers.EncodeAttribute(cell.FilterValue),
                        "'>",
                        cell.Html,
                        "</td>"));
                }

                _ = html.AppendLine("          </tr>");
            }
        }

        _ = html.AppendLine("        </tbody></table>");
        _ = html.AppendLine("    </div></div>");
        _ = html.AppendLine("  </div>");
        _ = html.AppendLine("</section>");
        return html.ToString();
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static TableRow BuildMetricRow(string metricName, int value) =>
        new(
        [
            BuildTextCell(metricName),
            BuildTextCell(value.ToString(CultureInfo.InvariantCulture), value)
        ]);

    private static TableCell BuildTextCell(string text, IFormattable? sortValue = null) =>
        new(
            HtmlPresentationHelpers.Encode(text),
            sortValue is null ? text : sortValue.ToString(null, CultureInfo.InvariantCulture),
            text);

    private static TableCell BuildLinkCell(string text, string url)
    {
        var encodedUrl = HtmlPresentationHelpers.EncodeAttribute(url);
        var encodedText = HtmlPresentationHelpers.Encode(text);
        return new TableCell(
            $"<a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noreferrer\">{encodedText}</a>",
            text,
            text);
    }

    private static IReadOnlyList<TableColumn> MetricColumns { get; } =
    [
        new TableColumn("Metric", "text", "Metric"),
        new TableColumn("Value", "number", "Value")
    ];

    private sealed record TableColumn(string Header, string SortType, string FilterPlaceholder, string? CssClass = null);

    private sealed record TableCell(string Html, string SortValue, string FilterValue);

    private sealed record TableRow(IReadOnlyList<TableCell> Cells, string? CssClass = null);
}
