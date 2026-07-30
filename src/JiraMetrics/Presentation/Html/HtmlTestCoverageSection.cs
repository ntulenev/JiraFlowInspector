using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the automated test-coverage HTML section.
/// </summary>
internal sealed class HtmlTestCoverageSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var testCoverage = RatioSectionPresentationData.Create(reportData).TestCoverage;
        if (testCoverage is null)
        {
            return string.Empty;
        }

        return BuildTableSection(
            "test-coverage",
            "Automated Test Coverage",
            "No automated test coverage data available.",
            MetricColumns,
            [
                BuildTextMetricRow("Issue Types", testCoverage.IssueTypesLabel),
                BuildTextMetricRow("Test Project", testCoverage.TestProjectLabel),
                BuildTextMetricRow("Link", testCoverage.LinkLabel),
                BuildMetricRow("Done in selected period", testCoverage.TotalIssues.Value),
                BuildMetricRow("Covered by automated tests", testCoverage.CoveredIssueCount.Value),
                new TableRow(
                [
                    BuildTextCell("Coverage"),
                    BuildTextCell(testCoverage.CoverageText, testCoverage.CoveragePercentage)
                ])
            ],
            defaultSortColumn: 0,
            compact: true);
    }
}
