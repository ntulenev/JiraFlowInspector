using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders transition path summary and detail HTML sections.
/// </summary>
internal sealed class HtmlPathGroupsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildPathSummaryTable(reportData.Transitions.PathSummary));
        _ = html.Append(HtmlContentComposer.BuildPathGroupsTable(reportData));
        return html.ToString();
    }
}
