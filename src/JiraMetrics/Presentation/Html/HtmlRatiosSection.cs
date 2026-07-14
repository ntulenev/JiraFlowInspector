using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders issue-ratio and test-coverage HTML sections.
/// </summary>
internal sealed class HtmlRatiosSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildRatiosSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildBugRatioDetailsSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildTestCoverageSection(reportData));
        return html.ToString();
    }
}
