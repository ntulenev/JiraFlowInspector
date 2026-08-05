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
        var sections = new List<string>(2);

        if (reportData.OptionalSectionFailures.Count > 0)
        {
            var optionalRows = reportData.OptionalSectionFailures
                .Select((failure, index) => new TableRow(
                [
                    BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                    BuildTextCell(OptionalReportSectionNames.GetDisplayName(failure.Section)),
                    BuildTextCell(failure.Error.Value)
                ]))
                .ToList();

            sections.Add(BuildTableSection(
                "optional-section-failures",
                "Optional Sections Unavailable",
                "All enabled optional sections were loaded.",
                [
                    new TableColumn("#", "number", "#", "narrow"),
                    new TableColumn("Section", "text", "Section", "issue-column"),
                    new TableColumn("Reason", "text", "Reason", "summary-column")
                ],
                optionalRows,
                defaultSortColumn: 0));
        }

        if (reportData.Failures.Count > 0)
        {
            var rows = reportData.Failures
                .Select((failure, index) => new TableRow(
                [
                    BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                    BuildLinkCell(failure.IssueKey.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, failure.IssueKey)),
                    BuildTextCell(failure.Reason.Value)
                ]))
                .ToList();

            sections.Add(BuildTableSection(
                "failures",
                "Failed Issues",
                "No failed issue loads.",
                [
                    new TableColumn("#", "number", "#", "narrow"),
                    new TableColumn("Issue", "text", "Issue", "issue-column"),
                    new TableColumn("Reason", "text", "Reason", "summary-column")
                ],
                rows,
                defaultSortColumn: 0));
        }

        return string.Concat(sections);
    }
}
