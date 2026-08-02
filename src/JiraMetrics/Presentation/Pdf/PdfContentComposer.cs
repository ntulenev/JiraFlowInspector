using JiraMetrics.Models;

using QuestPDF.Fluent;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Default PDF content composer for Jira analytics report.
/// </summary>
public sealed class PdfContentComposer : IPdfContentComposer
{

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfContentComposer"/> class.
    /// </summary>
    public PdfContentComposer()
        : this(
            [
                new PdfReleaseSection(),
                new PdfArchTasksSection(),
                new PdfGlobalIncidentsSection(),
                new PdfRatiosSection(),
                new PdfTestCoverageSection(),
                new PdfTransitionAnalysisSection(),
                new PdfQaTransitionAnalysisSection(),
                new PdfPathGroupsSection(),
                new PdfGeneralStatisticsSection(),
                new PdfUnresolved30DaysTasksSection(),
                new PdfFailuresSection(),
                new PdfCustomTransitionAnalysisSection()
            ])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfContentComposer"/> class
    /// with the provided report sections.
    /// </summary>
    /// <param name="sections">Ordered PDF report sections.</param>
    internal PdfContentComposer(IReadOnlyList<IPdfReportSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        Sections = sections;
    }

    internal IReadOnlyList<IPdfReportSection> Sections { get; }

    /// <inheritdoc />
    public void ComposeContent(ColumnDescriptor column, JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        column.Spacing(10);

        foreach (var section in Sections)
        {
            section.Compose(column, reportData);
        }
    }
}

