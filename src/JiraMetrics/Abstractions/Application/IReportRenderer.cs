using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Generates one configured report output format.
/// </summary>
public interface IReportRenderer
{
    /// <summary>
    /// Renders all report files owned by this renderer.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    /// <returns>Generated report outputs, or an empty collection when the format is disabled.</returns>
    IReadOnlyList<ReportOutput> RenderReport(JiraReportData reportData);
}
