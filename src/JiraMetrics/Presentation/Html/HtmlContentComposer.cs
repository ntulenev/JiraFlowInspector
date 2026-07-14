using System.Globalization;
using System.Text;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

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
    public string Compose(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var content = new StringBuilder(32 * 1024);
        foreach (var section in DefaultSections)
        {
            _ = content.Append(section.Compose(reportData));
        }
        return HtmlDocumentComposer.Compose(reportData, content.ToString());
    }

    internal static string BuildRatiosSection(JiraReportData reportData)
    {
        var rows = new List<TableRow>();
        AddRatioRows(rows, "All tasks", reportData.AllTasksCreatedThisMonth, reportData.AllTasksOpenThisMonth, reportData.AllTasksMovedToDoneThisMonth, reportData.AllTasksRejectedThisMonth, reportData.AllTasksFinishedThisMonth);
        AddRatioRows(rows, "Bugs", reportData.BugCreatedThisMonth, new ItemCount(reportData.BugOpenIssues.Count), reportData.BugMovedToDoneThisMonth, reportData.BugRejectedThisMonth, reportData.BugFinishedThisMonth);
        if (reportData.BugReporducedOnProd.HasValue)
        {
            rows.Add(BuildMetricRow("Bugs: Reproduced on prod", reportData.BugReporducedOnProd.Value.Value));
        }

        return BuildTableSection(
            "ratios",
            "Task Ratios",
            "No ratio data available.",
            MetricColumns,
            rows,
            defaultSortColumn: 0,
            compact: true);
    }

    internal static string BuildTestCoverageSection(JiraReportData reportData)
    {
        if (reportData.Settings.TestCoverage is not { Enabled: true } testCoverageSettings)
        {
            return string.Empty;
        }

        return BuildTableSection(
            "test-coverage",
            "Automated Test Coverage",
            "No automated test coverage data available.",
            MetricColumns,
            [
                BuildTextMetricRow(
                    "Issue Types",
                    string.Join(", ", testCoverageSettings.IssueTypes.Select(static issueType => issueType.Value))),
                BuildTextMetricRow("Test Project", testCoverageSettings.TestProjectKey.Value),
                BuildTextMetricRow("Link", testCoverageSettings.LinkName),
                BuildMetricRow("Done in selected period", reportData.TestCoverage.TotalIssues.Value),
                BuildMetricRow("Covered by automated tests", reportData.TestCoverage.CoveredIssueCount.Value),
                new TableRow(
                [
                    BuildTextCell("Coverage"),
                    BuildTextCell(PresentationFormatting.FormatPercentage(reportData.TestCoverage.CoveragePercentage), reportData.TestCoverage.CoveragePercentage)
                ])
            ],
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
            BuildTextCell(PresentationFormatting.BuildFinishedToCreatedRatioText(created.Value, finished.Value))
        ]));
    }

    internal static string BuildBugRatioDetailsSection(JiraReportData reportData)
    {
        if (!reportData.BugCreatedThisMonth.HasValue)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        _ = html.Append(BuildIssueListItemsTable(
            "bug-open-issues",
            "Bug Ratio: Open Issues",
            reportData.BugOpenIssues,
            reportData,
            includeCreatedAt: true,
            includeReporducedOnProd: reportData.BugReporducedOnProd.HasValue));
        _ = html.Append(BuildIssueListItemsTable(
            "bug-done-issues",
            "Bug Ratio: Done Issues",
            reportData.BugDoneIssues,
            reportData,
            includeCreatedAt: true,
            includeReporducedOnProd: reportData.BugReporducedOnProd.HasValue));
        _ = html.Append(BuildIssueListItemsTable(
            "bug-rejected-issues",
            "Bug Ratio: Rejected Issues",
            reportData.BugRejectedIssues,
            reportData,
            includeCreatedAt: false,
            includeReporducedOnProd: reportData.BugReporducedOnProd.HasValue));
        return html.ToString();
    }

    internal static string BuildQaTransitionAnalysisSection(JiraReportData reportData)
    {
        var analysis = reportData.QaTransitionAnalysis;
        if (analysis.AnalyzedIssueCount.Value == 0)
        {
            return string.Empty;
        }

        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;
        var html = new StringBuilder();
        _ = html.Append(BuildTableSection(
            "qa-summary",
            "QA Transition Analysis",
            "No QA transition data.",
            MetricColumns,
            [
                BuildTextMetricRow("Total Done Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.DoneIssues).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Total Rejected Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.RejectedIssues).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Open Bugs", reportData.BugOpenIssues.Count.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Open On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugOpenIssues)),
                BuildTextMetricRow("Done Bugs", reportData.BugDoneIssues.Count.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Done On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugDoneIssues)),
                BuildTextMetricRow("Rejected Bugs", reportData.BugRejectedIssues.Count.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Rejected On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugRejectedIssues)),
                BuildTextMetricRow("QA In Progress Coverage", QaTransitionPresentationSummary.BuildCoverageText(analysis)),
                BuildTextMetricRow("QA In Progress 75P", FormatDuration(analysis.PickupDuration75, showHoursOnly)),
                BuildTextMetricRow("QA Transition 75P", FormatDuration(analysis.TestingDuration75, showHoursOnly)),
                BuildTextMetricRow("QA Hold 75P", FormatDuration(analysis.HoldDuration75, showHoursOnly))
            ],
            defaultSortColumn: 0,
            compact: true));

        _ = html.Append(BuildTableSection(
            "qa-pickup-summary",
            "QA Pickup",
            "No QA pickup data.",
            [
                new TableColumn("Transition", "text", "Transition"),
                new TableColumn("Issues", "text", "Issues"),
                new TableColumn("Share", "number", "Share"),
                new TableColumn("75P", "number", "75P")
            ],
            [
                new TableRow(
                [
                    BuildTextCell(QaTransitionPresentationSummary.BuildRulesLabel(reportData.Settings.QaTransitionAnalysis.PickupTransitions)),
                    BuildTextCell($"{analysis.PickupIssues.Count.ToString(CultureInfo.InvariantCulture)}/{analysis.AnalyzedIssueCount.Value.ToString(CultureInfo.InvariantCulture)}"),
                    BuildTextCell(analysis.PickupIssuePercentage.ToString("0.##", CultureInfo.InvariantCulture) + "%", analysis.PickupIssuePercentage),
                    BuildTextCell(FormatDuration(analysis.PickupDuration75, showHoursOnly), analysis.PickupDuration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true));

        _ = html.Append(BuildIssueTypeDuration75Table("qa-pickup-75", "QA Pickup 75P per type", analysis.PickupDuration75PerType, showHoursOnly));
        _ = html.Append(BuildTransitionMeasurementTable("qa-testing-issues", "Testing time by issue", analysis.TestingIssues, reportData));
        _ = html.Append(BuildIssueTypeDuration75Table("qa-testing-75", "Testing time 75P per type", analysis.TestingDuration75PerType, showHoursOnly));
        _ = html.Append(BuildTableSection(
            "qa-hold-summary",
            "QA Hold",
            "No QA hold data.",
            [
                new TableColumn("Transition", "text", "Transition"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn("75P", "number", "75P")
            ],
            [
                new TableRow(
                [
                    BuildTextCell(QaTransitionPresentationSummary.BuildRulesLabel(reportData.Settings.QaTransitionAnalysis.HoldTransitions)),
                    BuildTextCell(analysis.HoldIssues.Count.ToString(CultureInfo.InvariantCulture), analysis.HoldIssues.Count),
                    BuildTextCell(FormatDuration(analysis.HoldDuration75, showHoursOnly), analysis.HoldDuration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 1,
            defaultSortDirection: "desc",
            compact: true));
        _ = html.Append(BuildTransitionMeasurementTable(
            "qa-hold-issues",
            "QA hold time by issue",
            analysis.HoldIssues,
            reportData,
            showHoursOnly ? "Hours on hold" : "Days on hold"));
        _ = html.Append(BuildIssueTypeDuration75Table("qa-hold-75", "QA hold 75P per type", analysis.HoldDuration75PerType, showHoursOnly));
        return html.ToString();
    }

    internal static string BuildGeneralStatisticsSection(JiraReportData reportData)
    {
        if (!reportData.Settings.ShowGeneralStatistics)
        {
            return string.Empty;
        }

        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        var excludedStatuses = reportData.Settings.RejectStatusName is { } rejectStatus
            ? $"{reportData.Settings.DoneStatusName.Value}, {rejectStatus.Value}"
            : reportData.Settings.DoneStatusName.Value;
        var rows = reportData.OpenIssuesByStatus
            .OrderByDescending(static summary => summary.Count.Value)
            .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase)
            .Select(summary => new TableRow(
            [
                BuildTextCell(summary.Status.Value),
                BuildTextCell(summary.Count.Value.ToString(CultureInfo.InvariantCulture), summary.Count.Value),
                BuildTextCell(BuildIssueTypeBreakdown(summary.IssueTypes))
            ]))
            .ToList();

        var note = string.Concat(
            "<section class=\"info-section\" id=\"general-statistics-note\">",
            "<div class=\"info-panel\"><strong>General Statistics is a current snapshot.</strong> ",
            "It is calculated from issues that are not currently in excluded statuses as of ",
            HtmlPresentationHelpers.Encode(generatedAt),
            ". It is not a historical period slice. Excluded statuses: ",
            HtmlPresentationHelpers.Encode(excludedStatuses),
            ".</div></section>");

        return note + BuildTableSection(
            "general-statistics",
            "General Statistics",
            "No issues outside excluded statuses.",
            [
                new TableColumn("Status", "text", "Status"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn("Breakdown by type", "text", "Breakdown", "summary-column")
            ],
            rows,
            defaultSortColumn: 1,
            defaultSortDirection: "desc");
    }

    internal static string BuildIssueTimelineTable(
        string sectionId,
        string title,
        IReadOnlyList<IssueTimeline> issues,
        StatusName targetStatusName,
        string atColumnTitle,
        JiraReportData reportData)
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
            new TableColumn(PresentationFormatting.GetWorkDurationColumnLabel(reportData.Settings.ShowTimeCalculationsInHoursOnly), "number", "Duration")
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
                BuildTextCell(PresentationFormatting.BuildLastStatusAtText(issue, targetStatusName), lastStatusAt?.ToUnixTimeSeconds()),
                BuildTextCell(
                    PresentationFormatting.BuildWorkDurationText(issue, targetStatusName, reportData.Settings.ShowTimeCalculationsInHoursOnly),
                    workDuration?.TotalMinutes)
            ]));
        }

        return BuildTableSection(sectionId, title, "No issues.", columns, rows, defaultSortColumn: 1);
    }

    internal static string BuildUnresolved30DaysTasksSection(JiraReportData reportData)
    {
        if (reportData.Settings.Unresolved30DaysTasksReport is null)
        {
            return string.Empty;
        }

        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        var note = string.Concat(
            "<section class=\"info-section\" id=\"unresolved-30-days-tasks-note\">",
            "<div class=\"info-panel\"><strong>Unresolved 30+ Days Tasks is a current snapshot.</strong> ",
            "It shows tasks matching the configured query as of ",
            HtmlPresentationHelpers.Encode(generatedAt),
            ". It is not a historical period slice.</div></section>");

        var rows = reportData.Unresolved30DaysTasks
            .OrderBy(static issue => issue.CreatedAt)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((issue, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(issue.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key)),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(issue.CreatedAt), issue.CreatedAt?.ToUnixTimeSeconds()),
                BuildTextCell(issue.IssueType ?? "-"),
                BuildTextCell(issue.Assignee ?? "Unassigned"),
                BuildTextCell(issue.Status ?? "-"),
                BuildTextCell(issue.Title.Value)
            ]))
            .ToList();

        return note + BuildTableSection(
            "unresolved-30-days-tasks",
            "Unresolved 30+ Days Tasks",
            "No unresolved tasks older than 30 days found.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Created", "number", "Created"),
                new TableColumn("Issue Type", "text", "Issue Type"),
                new TableColumn("Assignee", "text", "Assignee"),
                new TableColumn("Status", "text", "Status"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 2);
    }

    internal static string BuildDuration75PerTypeTable(
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
                    PresentationFormatting.FormatWorkDurationValue(summary.DaysAtWorkP75, showTimeCalculationsInHoursOnly),
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
                new TableColumn(PresentationFormatting.GetWorkDuration75Title(showTimeCalculationsInHoursOnly), "number", "75P")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

    internal static string BuildPathSummaryTable(PathGroupsSummary summary) =>
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

    internal static string BuildPathGroupsTable(JiraReportData reportData)
    {
        var columns = new[]
        {
            new TableColumn("#", "number", "#", "narrow"),
            new TableColumn("Issues", "number", "Issues"),
            new TableColumn("TTM 75P", "number", "TTM 75P"),
            new TableColumn("Transition Len", "number", "Transition Len")
        };
        var html = new StringBuilder();
        _ = html.AppendLine("<section class=\"table-section\" id=\"path-groups\">");
        _ = html.AppendLine("  <div class=\"section-header\"><h2>Path Groups</h2></div>");
        _ = html.AppendLine("  <div class=\"table-panel\" data-table-panel>");
        _ = html.AppendLine("    <div class=\"table-controls\">");
        _ = html.AppendLine("      <input class=\"search\" data-table-search type=\"search\" placeholder=\"Search this table\">");
        _ = html.AppendLine("      <button class=\"button\" data-table-reset type=\"button\">Reset Filters</button>");
        _ = html.AppendLine("    </div>");
        _ = html.AppendLine("    <div class=\"table-wrap\"><div class=\"scroll\">");
        _ = html.AppendLine("      <table class=\"report-table path-table\" data-default-sort-column=\"1\" data-default-sort-direction=\"desc\">");
        _ = html.AppendLine("        <thead><tr>");

        for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
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
        if (reportData.PathGroups.Count == 0)
        {
            _ = html.AppendLine("          <tr class=\"empty\"><td class=\"empty-cell\" colspan=\"4\">No path groups.</td></tr>");
        }
        else
        {
            for (var index = 0; index < reportData.PathGroups.Count; index++)
            {
                var group = reportData.PathGroups[index];
                var groupNumber = index + 1;
                var filterValue = string.Join(
                    " ",
                    new[] { group.PathLabel.Value }.Concat(group.Issues.Select(static issue => issue.Key.Value)));
                _ = html.AppendLine("          <tr class=\"path-group-row\" data-toggle-detail>");
                _ = html.AppendLine(string.Concat(
                    "            <td data-sort='",
                    groupNumber.ToString(CultureInfo.InvariantCulture),
                    "' data-filter='",
                    HtmlPresentationHelpers.EncodeAttribute(filterValue),
                    "'><button class=\"row-toggle\" type=\"button\" aria-label=\"Toggle path group details\">+</button> ",
                    groupNumber.ToString(CultureInfo.InvariantCulture),
                    "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.Issues.Count.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", group.Issues.Count.ToString(CultureInfo.InvariantCulture), "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.TotalP75.TotalMinutes.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", HtmlPresentationHelpers.Encode(PdfPresentationHelpers.ToDurationLabel(group.TotalP75, reportData.Settings.ShowTimeCalculationsInHoursOnly)), "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.P75Transitions.Count.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", group.P75Transitions.Count.ToString(CultureInfo.InvariantCulture), "</td>"));
                _ = html.AppendLine("          </tr>");
                _ = html.AppendLine("          <tr class=\"detail-row\" hidden><td colspan=\"4\">");
                _ = html.Append(BuildPathGroupDetails(group, reportData));
                _ = html.AppendLine("          </td></tr>");
            }
        }

        _ = html.AppendLine("        </tbody></table>");
        _ = html.AppendLine("    </div></div>");
        _ = html.AppendLine("  </div>");
        _ = html.AppendLine("</section>");
        return html.ToString();
    }

    internal static string BuildReleaseTable(JiraReportData reportData)
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

    internal static string BuildComponentsReleaseTable(JiraReportData reportData)
    {
        if (reportData.Settings.ReleaseReport is null
            || string.IsNullOrWhiteSpace(reportData.Settings.ReleaseReport.ComponentsFieldName))
        {
            return string.Empty;
        }

        var rows = PresentationFormatting.BuildComponentReleaseSummaries(reportData.ReleaseIssues)
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildTextCell(item.componentName),
                BuildTextCell(item.releaseCount.ToString(CultureInfo.InvariantCulture), item.releaseCount)
            ]))
            .ToList();

        return BuildTableSection(
            "components-release-table",
            "Components Release Table",
            "No components data.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Component name", "text", "Component"),
                new TableColumn("Release counts", "number", "Release counts")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

    private static string BuildPathGroupDetails(PathGroup group, JiraReportData reportData)
    {
        var html = new StringBuilder();
        var maxDuration = group.P75Transitions.Count == 0
            ? 0
            : group.P75Transitions.Max(static transition => Math.Max(0.001, transition.P75Duration.TotalMinutes));

        _ = html.AppendLine("            <div class=\"path-detail\">");
        _ = html.AppendLine(string.Concat("              <div class=\"path-label\">", HtmlPresentationHelpers.Encode(group.PathLabel.Value), "</div>"));
        _ = html.AppendLine("              <div class=\"path-detail-grid\">");
        _ = html.AppendLine("                <div>");
        _ = html.AppendLine("                  <h3>Tasks</h3>");
        _ = html.AppendLine("                  <div class=\"detail-list\">");
        foreach (var issue in group.Issues.OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase))
        {
            var issueUrl = HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key);
            _ = html.AppendLine(string.Concat(
                "                    <div><a href=\"",
                HtmlPresentationHelpers.EncodeAttribute(issueUrl),
                "\" target=\"_blank\" rel=\"noreferrer\">",
                HtmlPresentationHelpers.Encode(issue.Key.Value),
                "</a><span>",
                HtmlPresentationHelpers.Encode(issue.IssueType.Value),
                "</span><span>",
                HtmlPresentationHelpers.Encode(issue.Summary.Value),
                "</span></div>"));
        }

        _ = html.AppendLine("                  </div>");
        _ = html.AppendLine("                </div>");
        _ = html.AppendLine("                <div>");
        _ = html.AppendLine("                  <h3>Transitions</h3>");
        if (group.P75Transitions.Count == 0)
        {
            _ = html.AppendLine("                  <p class=\"empty-section\">No transitions in this path.</p>");
        }
        else
        {
            _ = html.AppendLine("                  <div class=\"transition-bars\">");
            foreach (var transition in group.P75Transitions)
            {
                var duration = transition.P75Duration < TimeSpan.Zero ? TimeSpan.Zero : transition.P75Duration;
                var width = maxDuration <= 0
                    ? 0
                    : Math.Clamp(duration.TotalMinutes / maxDuration * 100, 4, 100);
                _ = html.AppendLine("                    <div class=\"transition-bar-row\">");
                _ = html.AppendLine(string.Concat(
                    "                      <div class=\"transition-label\">",
                    HtmlPresentationHelpers.Encode(transition.From.Value),
                    " -> ",
                    HtmlPresentationHelpers.Encode(transition.To.Value),
                    "</div>"));
                _ = html.AppendLine("                      <div class=\"bar-track\">");
                _ = html.AppendLine(string.Concat(
                    "                        <div class=\"bar-fill\" style=\"width:",
                    width.ToString("0.##", CultureInfo.InvariantCulture),
                    "%\"></div>"));
                _ = html.AppendLine("                      </div>");
                _ = html.AppendLine(string.Concat(
                    "                      <div class=\"duration-value\">",
                    HtmlPresentationHelpers.Encode(FormatDurationWithHours(duration, reportData.Settings.ShowTimeCalculationsInHoursOnly)),
                    "</div>"));
                _ = html.AppendLine("                    </div>");
            }

            _ = html.AppendLine("                  </div>");
        }

        _ = html.AppendLine("                </div>");
        _ = html.AppendLine("              </div>");
        _ = html.AppendLine("            </div>");
        return html.ToString();
    }

    internal static string BuildArchTasksTable(JiraReportData reportData)
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

    internal static string BuildGlobalIncidentsTable(JiraReportData reportData)
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
                BuildTextCell(PresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentStartUtc), incident.IncidentStartUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc), incident.IncidentRecoveryUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatIncidentDuration(incident.Duration, reportData.Settings.ShowTimeCalculationsInHoursOnly), incident.Duration?.TotalMinutes),
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

    internal static string BuildFailuresTable(JiraReportData reportData)
    {
        if (reportData.Failures.Count == 0)
        {
            return string.Empty;
        }

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

    internal static string BuildRoadmapSection(JiraReportData reportData)
    {
        if (reportData.Settings.RoadmapReport is null)
        {
            return string.Empty;
        }

        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        var note = string.Concat(
            "<section class=\"info-section\" id=\"roadmap-note\">",
            "<div class=\"info-panel\"><strong>Roadmap is a current snapshot.</strong> ",
            "It shows issues matching the configured query as of ",
            HtmlPresentationHelpers.Encode(generatedAt),
            ". It is not built from historical data and does not represent a historical period slice.",
            "</div></section>");
        var rows = reportData.RoadmapItems
            .OrderBy(static item => item.StartDate)
            .ThenBy(static item => item.EndDate)
            .ThenBy(static item => item.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(item.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, item.Key)),
                BuildTextCell(item.Status),
                BuildTextCell(item.Roadmap ?? "-"),
                BuildTextCell(FormatRoadmapDate(item.StartDate)),
                BuildTextCell(FormatRoadmapDate(item.EndDate)),
                BuildTextCell(item.Summary.Value)
            ]))
            .ToList();

        return note + BuildTableSection(
            "roadmap",
            "Roadmap",
            "No roadmap issues found.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Status", "text", "Status", FilterKind: "multi-select"),
                new TableColumn("Roadmap", "text", "Roadmap", FilterKind: "multi-select"),
                new TableColumn("Start Date", "text", "Start Date", FilterKind: "date-range"),
                new TableColumn("End Date", "text", "End Date", FilterKind: "date-range"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 4);
    }

    private static string BuildIssueListItemsTable(
        string sectionId,
        string title,
        IReadOnlyList<IssueListItem> issues,
        JiraReportData reportData,
        bool includeCreatedAt,
        bool includeReporducedOnProd)
    {
        var columns = new List<TableColumn>
        {
            new("#", "number", "#", "narrow"),
            new("Issue", "text", "Issue", "issue-column")
        };
        if (includeCreatedAt)
        {
            columns.Add(new TableColumn("Created", "number", "Created"));
        }

        if (includeReporducedOnProd)
        {
            columns.Add(new TableColumn("Prod", "text", "Prod"));
            columns.Add(new TableColumn("Priority", "text", "Priority"));
        }

        columns.Add(new TableColumn("Title", "text", "Title", "summary-column"));

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rows = new List<TableRow>(orderedIssues.Length);

        for (var index = 0; index < orderedIssues.Length; index++)
        {
            var issue = orderedIssues[index];
            var cells = new List<TableCell>
            {
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(issue.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key))
            };
            if (includeCreatedAt)
            {
                cells.Add(BuildTextCell(HtmlPresentationHelpers.FormatDateTime(issue.CreatedAt), issue.CreatedAt?.ToUnixTimeSeconds()));
            }

            if (includeReporducedOnProd)
            {
                cells.Add(BuildTextCell(issue.ReporducedOnProd ? "Yes" : "No"));
                cells.Add(BuildTextCell(issue.Priority ?? "-"));
            }

            cells.Add(BuildTextCell(issue.Title.Value));
            rows.Add(new TableRow(cells, issue.ReporducedOnProd ? "warning-row" : null));
        }

        return BuildTableSection(sectionId, title, "No issues.", columns, rows, defaultSortColumn: 1);
    }

    private static string BuildTransitionMeasurementTable(
        string sectionId,
        string title,
        IReadOnlyList<TransitionMeasurementIssue> issues,
        JiraReportData reportData,
        string? durationColumnTitle = null)
    {
        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;
        var rows = issues
            .OrderByDescending(static item => item.Duration)
            .ThenBy(static item => item.Issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(item.Issue.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, item.Issue.Key)),
                BuildTextCell(item.Issue.IssueType.Value),
                BuildTextCell(item.Issue.SubItemsCount.ToString(CultureInfo.InvariantCulture), item.Issue.SubItemsCount),
                BuildTextCell(item.Issue.HasPullRequest ? "+" : string.Empty),
                BuildTextCell(item.Issue.Summary.Value),
                BuildTextCell(item.Rule.Label),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(item.TransitionAt), item.TransitionAt.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatWorkDurationValue(item.Duration, showHoursOnly), item.Duration.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No issues.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Sub-items", "number", "Sub-items"),
                new TableColumn("Code", "text", "Code"),
                new TableColumn("Summary", "text", "Summary", "summary-column"),
                new TableColumn("Measured transition", "text", "Measured transition"),
                new TableColumn("Transition At", "number", "Transition At"),
                new TableColumn(durationColumnTitle ?? (showHoursOnly ? "Hours in QA" : "Days in QA"), "number", "Duration")
            ],
            rows,
            defaultSortColumn: 8,
            defaultSortDirection: "desc");
    }

    private static string BuildIssueTypeDuration75Table(
        string sectionId,
        string title,
        IReadOnlyList<IssueTypeDuration75Summary> summaries,
        bool showTimeCalculationsInHoursOnly)
    {
        var rows = summaries
            .OrderByDescending(static summary => summary.DurationP75)
            .ThenByDescending(static summary => summary.IssueCount.Value)
            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(summary => new TableRow(
            [
                BuildTextCell(summary.IssueType.Value),
                BuildTextCell(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture), summary.IssueCount.Value),
                BuildTextCell(
                    PresentationFormatting.FormatWorkDurationValue(summary.DurationP75, showTimeCalculationsInHoursOnly),
                    summary.DurationP75.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No data.",
            [
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn(showTimeCalculationsInHoursOnly ? "Hours 75P" : "Days 75P", "number", "75P")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

    private static string BuildTableSection(
        string sectionId,
        string title,
        string emptyMessage,
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        int? defaultSortColumn,
        string defaultSortDirection = "asc",
        bool compact = false) =>
        HtmlTableRenderer.BuildTableSection(
            sectionId,
            title,
            emptyMessage,
            columns,
            rows,
            defaultSortColumn,
            defaultSortDirection,
            compact);

    private static TableRow BuildMetricRow(string metricName, int value) =>
        new(
        [
            BuildTextCell(metricName),
            BuildTextCell(value.ToString(CultureInfo.InvariantCulture), value)
        ]);

    private static TableRow BuildTextMetricRow(string metricName, string value) =>
        new(
        [
            BuildTextCell(metricName),
            BuildTextCell(value)
        ]);

    private static string FormatDuration(TimeSpan? duration, bool showTimeCalculationsInHoursOnly) =>
        duration is null
            ? "-"
            : PresentationFormatting.FormatWorkDurationValue(duration.Value, showTimeCalculationsInHoursOnly);

    private static string FormatRoadmapDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    private static string FormatDurationWithHours(TimeSpan duration, bool showTimeCalculationsInHoursOnly)
    {
        var durationLabel = PdfPresentationHelpers.ToDurationLabel(duration, showTimeCalculationsInHoursOnly);
        var hoursLabel = duration.TotalHours.ToString("0.##", CultureInfo.InvariantCulture) + "h";
        return showTimeCalculationsInHoursOnly
            ? durationLabel
            : $"{durationLabel} ({hoursLabel})";
    }

    private static string BuildIssueTypeBreakdown(IReadOnlyList<IssueTypeCountSummary> issueTypes) =>
        issueTypes.Count == 0
            ? "-"
            : string.Join(
                "; ",
                issueTypes
                    .OrderByDescending(static summary => summary.Count.Value)
                    .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                    .Select(static summary => $"{summary.IssueType.Value} - {summary.Count.Value.ToString(CultureInfo.InvariantCulture)}"));

    private static IReadOnlyList<TableColumn> MetricColumns { get; } =
    [
        new TableColumn("Metric", "text", "Metric"),
        new TableColumn("Value", "number", "Value")
    ];

    private static IReadOnlyList<IHtmlReportSection> DefaultSections { get; } =
    [
        new HtmlGlobalIncidentsSection(),
        new HtmlRatiosSection(),
        new HtmlQaTransitionAnalysisSection(),
        new HtmlIssueTimelineSection(),
        new HtmlPathGroupsSection(),
        new HtmlReleaseSection(),
        new HtmlArchTasksSection(),
        new HtmlGeneralStatisticsSection(),
        new HtmlUnresolved30DaysTasksSection(),
        new HtmlFailuresSection(),
        new HtmlRoadmapSection()
    ];

}
