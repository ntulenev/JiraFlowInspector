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
        return BuildTableSection(
            "finished-tasks",
            "Finished Tasks",
            "No finished task data available.",
            MetricColumns,
            [
                BuildMetricRow("Finished Tasks", finishedIssueCount),
                BuildMetricRow("Moved to Done", transitions.DoneIssues.Count),
                BuildMetricRow("Moved to Rejected", transitions.RejectedIssues.Count)
            ],
            defaultSortColumn: null,
            compact: true);
    }
}
