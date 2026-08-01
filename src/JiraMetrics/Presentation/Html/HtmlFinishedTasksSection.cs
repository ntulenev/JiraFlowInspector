using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the aggregate summary for finished-task report sections.
/// </summary>
internal sealed class HtmlFinishedTasksSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var transitions = reportData.Transitions;
        var finishedIssueCount = transitions.DoneIssues
            .Concat(transitions.RejectedIssues)
            .Select(static issue => issue.Key.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var duration75SampleIssueCount = transitions.DoneDaysAtWork75PerType
            .Sum(static summary => summary.IssueCount.Value);

        return BuildTableSection(
            "finished-tasks",
            "Finished Tasks",
            "No finished task data available.",
            MetricColumns,
            [
                BuildMetricRow("Finished Tasks", finishedIssueCount),
                BuildMetricRow("Moved to Done", transitions.DoneIssues.Count),
                BuildMetricRow("Moved to Rejected", transitions.RejectedIssues.Count),
                BuildMetricRow("Issue Types with 75P", transitions.DoneDaysAtWork75PerType.Count),
                BuildMetricRow("Issues in 75P Sample", duration75SampleIssueCount),
                BuildMetricRow("Successful Path Analyses", transitions.PathSummary.SuccessfulCount.Value),
                BuildMetricRow("Matched Stage", transitions.PathSummary.MatchedStageCount.Value),
                BuildMetricRow("Failed Path Analyses", transitions.PathSummary.FailedCount.Value),
                BuildMetricRow("Path Groups", transitions.PathSummary.PathGroupCount.Value)
            ],
            defaultSortColumn: null,
            compact: true);
    }
}
