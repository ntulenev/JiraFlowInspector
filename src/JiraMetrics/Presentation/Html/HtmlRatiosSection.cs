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
        var html = new StringBuilder();
        _ = html.Append(BuildRatiosSection(reportData));
        _ = html.Append(BuildBugRatioDetailsSection(reportData));
        _ = html.Append(BuildTestCoverageSection(reportData));
        return html.ToString();
    }

    private static string BuildBugRatioDetailsSection(JiraReportData reportData)
    {
        if (reportData.Ratios.Bugs is not { } bugRatio)
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

    private static string BuildRatiosSection(JiraReportData reportData)
    {
        var rows = new List<TableRow>();
        AddRatioRows(rows, "All tasks", reportData.Ratios.AllTasks);
        AddRatioRows(rows, "Bugs", reportData.Ratios.Bugs);
        if (reportData.Ratios.Bugs is { } bugRatio)
        {
            rows.Add(BuildMetricRow("Bugs: Reproduced on prod", bugRatio.ReporducedOnProdIssues.Count));
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

    private static string BuildTestCoverageSection(JiraReportData reportData)
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
                BuildMetricRow("Done in selected period", reportData.Ratios.TestCoverage.TotalIssues.Value),
                BuildMetricRow("Covered by automated tests", reportData.Ratios.TestCoverage.CoveredIssueCount.Value),
                new TableRow(
                [
                    BuildTextCell("Coverage"),
                    BuildTextCell(
                        PresentationFormatting.FormatPercentage(reportData.Ratios.TestCoverage.CoveragePercentage),
                        reportData.Ratios.TestCoverage.CoveragePercentage)
                ])
            ],
            defaultSortColumn: 0,
            compact: true);
    }

    private static void AddRatioRows(
        List<TableRow> rows,
        string scope,
        IssueRatioSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        rows.Add(BuildMetricRow($"{scope}: Created", snapshot.CreatedThisMonth.Value));
        rows.Add(BuildMetricRow($"{scope}: Open", snapshot.OpenThisMonth.Value));
        rows.Add(BuildMetricRow($"{scope}: Done", snapshot.MovedToDoneThisMonth.Value));
        rows.Add(BuildMetricRow($"{scope}: Rejected", snapshot.RejectedThisMonth.Value));
        rows.Add(BuildMetricRow($"{scope}: Finished", snapshot.FinishedThisMonth.Value));
        rows.Add(new TableRow(
        [
            BuildTextCell($"{scope}: Finished / Created"),
            BuildTextCell(PresentationFormatting.BuildFinishedToCreatedRatioText(
                snapshot.CreatedThisMonth,
                snapshot.FinishedThisMonth))
        ]));
    }
}
