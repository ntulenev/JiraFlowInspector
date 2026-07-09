using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Html;

/// <summary>
/// Composes standalone HTML for the Jira report.
/// </summary>
public interface IHtmlContentComposer
{
    /// <summary>
    /// Composes report HTML.
    /// </summary>
    /// <param name="reportData">Aggregated report data.</param>
    /// <returns>Standalone HTML document.</returns>
    string Compose(JiraPdfReportData reportData);
}
