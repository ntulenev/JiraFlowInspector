using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders one ordered section of the HTML report.
/// </summary>
internal interface IHtmlReportSection
{
    /// <summary>
    /// Renders section HTML.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    /// <returns>Section HTML.</returns>
    string Compose(JiraReportData reportData);
}
