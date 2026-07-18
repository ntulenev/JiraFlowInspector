using System.Text;

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
        _ = html.Append(HtmlContentComposer.BuildBugRatioDetailsSection(reportData));
        _ = html.Append(BuildTestCoverageSection(reportData));
        return html.ToString();
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
