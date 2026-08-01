using System.Globalization;
using System.Text;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders detailed bug-ratio HTML sections.
/// </summary>
internal sealed class HtmlBugRatiosSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var presentationData = RatioSectionPresentationData.Create(reportData);
        if (presentationData.Bugs is not { } bugRatio)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        _ = html.Append(BuildIssueListItemsTable(
            "bug-open-issues",
            "Bug Ratio: Open Issues",
            bugRatio.OpenIssues,
            reportData,
            includeCreatedAt: true,
            includeReporducedOnProd: true));
        _ = html.Append(BuildIssueListItemsTable(
            "bug-done-issues",
            "Bug Ratio: Done Issues",
            bugRatio.DoneIssues,
            reportData,
            includeCreatedAt: true,
            includeReporducedOnProd: true));
        _ = html.Append(BuildIssueListItemsTable(
            "bug-rejected-issues",
            "Bug Ratio: Rejected Issues",
            bugRatio.RejectedIssues,
            reportData,
            includeCreatedAt: false,
            includeReporducedOnProd: true));
        return html.ToString();
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

        var rows = new List<TableRow>(issues.Count);
        for (var index = 0; index < issues.Count; index++)
        {
            var issue = issues[index];
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
}
