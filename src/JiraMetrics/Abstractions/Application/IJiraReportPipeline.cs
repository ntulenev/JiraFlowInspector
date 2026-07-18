using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Generates all configured report outputs for a completed Jira analysis.
/// </summary>
public interface IJiraReportPipeline
{
    /// <summary>
    /// Renders all configured report outputs.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    void RenderReport(JiraReportData reportData);
}
