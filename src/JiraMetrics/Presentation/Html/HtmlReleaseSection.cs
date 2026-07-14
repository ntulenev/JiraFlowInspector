using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders release and component-release HTML sections.
/// </summary>
internal sealed class HtmlReleaseSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildReleaseTable(reportData));
        _ = html.Append(HtmlContentComposer.BuildComponentsReleaseTable(reportData));
        return html.ToString();
    }
}
