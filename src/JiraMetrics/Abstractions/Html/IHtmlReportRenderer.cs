using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Html;

/// <summary>
/// Renders Jira HTML report output.
/// </summary>
public interface IHtmlReportRenderer
{
    /// <summary>
    /// Renders and saves HTML report.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    void RenderReport(JiraReportData reportData);
}
