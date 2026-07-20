using System.Globalization;
using System.Text;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders issue timeline and duration-percentile HTML sections.
/// </summary>
internal sealed class HtmlIssueTimelineSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(BuildIssueTimelineTable(
            "done-issues",
            "Issues moved to Done in selected period",
            reportData.Transitions.DoneIssues,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData));
        _ = html.Append(BuildDuration75PerTypeTable(
            "done-duration-75",
            $"{PresentationFormatting.GetWorkDuration75Title(reportData.Settings.ShowTimeCalculationsInHoursOnly)} per type",
            reportData.Transitions.DoneDaysAtWork75PerType,
            reportData.Settings.ShowTimeCalculationsInHoursOnly));

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            _ = html.Append(BuildIssueTimelineTable(
                "rejected-issues",
                "Issues moved to Rejected in selected period",
                reportData.Transitions.RejectedIssues,
                rejectStatusName,
                "Rejected At",
                reportData));
        }

        return html.ToString();
    }

    private static string BuildIssueTimelineTable(
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
}
