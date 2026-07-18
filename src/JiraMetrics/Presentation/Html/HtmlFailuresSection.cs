using System.Globalization;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the issue-loading failures HTML section.
/// </summary>
internal sealed class HtmlFailuresSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
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
}
