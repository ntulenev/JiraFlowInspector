using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the architecture-tasks HTML section.
/// </summary>
internal sealed class HtmlArchTasksSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildArchTasksTable(reportData);
}
