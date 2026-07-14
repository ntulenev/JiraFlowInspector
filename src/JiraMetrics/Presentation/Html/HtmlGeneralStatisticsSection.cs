using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the general-statistics HTML section.
/// </summary>
internal sealed class HtmlGeneralStatisticsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGeneralStatisticsSection(reportData);
}
