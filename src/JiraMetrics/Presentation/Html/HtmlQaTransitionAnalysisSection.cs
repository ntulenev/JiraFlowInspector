using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the QA transition-analysis HTML section.
/// </summary>
internal sealed class HtmlQaTransitionAnalysisSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildQaTransitionAnalysisSection(reportData);
}
