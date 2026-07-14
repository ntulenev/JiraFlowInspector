using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the global-incidents HTML section.
/// </summary>
internal sealed class HtmlGlobalIncidentsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGlobalIncidentsTable(reportData);
}
