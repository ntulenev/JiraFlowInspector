using JiraMetrics.Models;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Renders Jira analytics PDF report.
/// </summary>
public interface IPdfReportRenderer
{
    /// <summary>
    /// Renders and saves PDF report.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    void RenderReport(JiraPdfReportData reportData);
}
