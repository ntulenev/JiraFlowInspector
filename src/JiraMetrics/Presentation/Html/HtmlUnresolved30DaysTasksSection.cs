using System.Globalization;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the current snapshot of unresolved tasks older than 30 days.
/// </summary>
internal sealed class HtmlUnresolved30DaysTasksSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (reportData.Settings.Unresolved30DaysTasksReport is null)
        {
            return string.Empty;
        }

        var generatedAt = reportData.RunContext.GeneratedAt.ToString(
            "yyyy-MM-dd HH:mm:ss zzz",
            CultureInfo.InvariantCulture);
        var note = string.Concat(
            "<section class=\"info-section\" id=\"unresolved-30-days-tasks-note\">",
            "<div class=\"info-panel\"><strong>Unresolved 30+ Days Tasks is a current snapshot.</strong> ",
            "It shows tasks matching the configured query as of ",
            HtmlPresentationHelpers.Encode(generatedAt),
            ". It is not a historical period slice.</div></section>");

        var rows = reportData.Source.Unresolved30DaysTasks
            .OrderBy(static issue => issue.CreatedAt)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((issue, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(
                    issue.Key.Value,
                    HtmlPresentationHelpers.BuildIssueBrowseUrl(
                        reportData.Settings.BaseUrl,
                        issue.Key)),
                BuildTextCell(
                    HtmlPresentationHelpers.FormatDateTime(issue.CreatedAt),
                    issue.CreatedAt?.ToUnixTimeSeconds()),
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
                new TableColumn("Issue Type", "text", "Issue Type", FilterKind: "multi-select"),
                new TableColumn("Assignee", "text", "Assignee"),
                new TableColumn("Status", "text", "Status", FilterKind: "multi-select"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 2);
    }
}
