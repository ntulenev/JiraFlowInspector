using System.Globalization;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the architecture-tasks HTML section.
/// </summary>
internal sealed class HtmlArchTasksSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        if (reportData.Settings.ArchTasksReport is null)
        {
            return string.Empty;
        }

        var rows = reportData.Source.ArchTasks
            .OrderBy(static task => task.CreatedAt)
            .ThenBy(static task => task.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((task, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(
                    task.Key.Value,
                    HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, task.Key)),
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
}
