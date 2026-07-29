using System.Text;
using System.Globalization;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders issue-ratio and test-coverage HTML sections.
/// </summary>
internal sealed class HtmlRatiosSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var presentationData = RatioSectionPresentationData.Create(reportData);
        var html = new StringBuilder();
        _ = html.Append(BuildRatiosSection(presentationData));
        _ = html.Append(BuildBugRatioDetailsSection(presentationData, reportData));
        _ = html.Append(BuildTestCoverageSection(presentationData));
        return html.ToString();
    }

    private static string BuildBugRatioDetailsSection(
        RatioSectionPresentationData presentationData,
        JiraReportData reportData)
    {
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

    private static string BuildRatiosSection(RatioSectionPresentationData presentationData)
    {
        var rows = new List<TableRow>();
        AddRatioRows(rows, "All tasks", presentationData.AllTasks);
        AddRatioRows(rows, "Bugs", presentationData.Bugs);
        if (presentationData.Bugs is { } bugRatio)
        {
            rows.Add(BuildMetricRow("Bugs: Reproduced on prod", bugRatio.ReproducedOnProdCount.Value));
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

    private static string BuildTestCoverageSection(RatioSectionPresentationData presentationData)
    {
        if (presentationData.TestCoverage is not { } testCoverage)
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
                    testCoverage.IssueTypesLabel),
                BuildTextMetricRow("Test Project", testCoverage.TestProjectLabel),
                BuildTextMetricRow("Link", testCoverage.LinkLabel),
                BuildMetricRow("Done in selected period", testCoverage.TotalIssues.Value),
                BuildMetricRow("Covered by automated tests", testCoverage.CoveredIssueCount.Value),
                new TableRow(
                [
                    BuildTextCell("Coverage"),
                    BuildTextCell(
                        testCoverage.CoverageText,
                        testCoverage.CoveragePercentage)
                ])
            ],
            defaultSortColumn: 0,
            compact: true);
    }

    private static void AddRatioRows(
        List<TableRow> rows,
        string scope,
        IssueRatioPresentationData? snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        rows.Add(BuildMetricRow($"{scope}: Created", snapshot.CreatedCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Open", snapshot.OpenCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Done", snapshot.DoneCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Rejected", snapshot.RejectedCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Finished", snapshot.FinishedCount.Value));
        rows.Add(new TableRow(
        [
            BuildTextCell($"{scope}: Finished / Created"),
            BuildTextCell(snapshot.FinishedToCreatedRatioText)
        ]));
    }
}
