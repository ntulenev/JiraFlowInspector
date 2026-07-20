using System.Globalization;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the general-statistics HTML section.
/// </summary>
internal sealed class HtmlGeneralStatisticsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        if (!reportData.Settings.ShowGeneralStatistics)
        {
            return string.Empty;
        }

        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        var excludedStatuses = reportData.Settings.RejectStatusName is { } rejectStatus
            ? $"{reportData.Settings.DoneStatusName.Value}, {rejectStatus.Value}"
            : reportData.Settings.DoneStatusName.Value;
        var rows = reportData.Source.OpenIssuesByStatus
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

    private static string BuildIssueTypeBreakdown(IReadOnlyList<IssueTypeCountSummary> issueTypes) =>
        issueTypes.Count == 0
            ? "-"
            : string.Join(
                "; ",
                issueTypes
                    .OrderByDescending(static summary => summary.Count.Value)
                    .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                    .Select(static summary => $"{summary.IssueType.Value} - {summary.Count.Value.ToString(CultureInfo.InvariantCulture)}"));
}
