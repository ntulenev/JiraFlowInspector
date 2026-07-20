using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Composes standalone HTML from ordered report sections.
/// </summary>
public sealed class HtmlContentComposer : IHtmlContentComposer
{
    /// <summary>
    /// Initializes a composer with the default report sections.
    /// </summary>
    public HtmlContentComposer()
        : this(
            [
                new HtmlGlobalIncidentsSection(),
                new HtmlRatiosSection(),
                new HtmlQaTransitionAnalysisSection(),
                new HtmlIssueTimelineSection(),
                new HtmlPathGroupsSection(),
                new HtmlReleaseSection(),
                new HtmlArchTasksSection(),
                new HtmlGeneralStatisticsSection(),
                new HtmlUnresolved30DaysTasksSection(),
                new HtmlFailuresSection(),
                new HtmlRoadmapSection()
            ])
    {
    }

    internal HtmlContentComposer(IReadOnlyList<IHtmlReportSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _sections = sections;
    }

    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var content = new StringBuilder(32 * 1024);
        foreach (var section in _sections)
        {
            _ = content.Append(section.Compose(reportData));
        }

        return HtmlDocumentComposer.Compose(reportData, content.ToString());
    }

    private readonly IReadOnlyList<IHtmlReportSection> _sections;
}
