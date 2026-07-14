using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the issue-loading failures HTML section.
/// </summary>
internal sealed class HtmlFailuresSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildFailuresTable(reportData);
}
