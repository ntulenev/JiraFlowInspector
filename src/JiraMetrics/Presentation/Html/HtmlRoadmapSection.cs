using System.Globalization;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the current Jira roadmap snapshot.
/// </summary>
internal sealed class HtmlRoadmapSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (reportData.Settings.RoadmapReport is null)
        {
            return string.Empty;
        }

        var generatedAt = reportData.RunContext.GeneratedAt.ToString(
            "yyyy-MM-dd HH:mm:ss zzz",
            CultureInfo.InvariantCulture);
        var note = string.Concat(
            "<section class=\"info-section\" id=\"roadmap-note\">",
            "<div class=\"info-panel\"><strong>Roadmap is a current snapshot.</strong> ",
            "It shows issues matching the configured query as of ",
            HtmlPresentationHelpers.Encode(generatedAt),
            ". It is not built from historical data and does not represent a historical period slice.",
            "</div></section>");
        var rows = reportData.Source.RoadmapItems
            .OrderBy(static item => item.StartDate)
            .ThenBy(static item => item.EndDate)
            .ThenBy(static item => item.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(
                    item.Key.Value,
                    HtmlPresentationHelpers.BuildIssueBrowseUrl(
                        reportData.Settings.BaseUrl,
                        item.Key)),
                BuildTextCell(item.Status),
                BuildTextCell(item.Roadmap ?? "-"),
                BuildTextCell(FormatDate(item.StartDate)),
                BuildTextCell(FormatDate(item.EndDate)),
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

    private static string FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
}
