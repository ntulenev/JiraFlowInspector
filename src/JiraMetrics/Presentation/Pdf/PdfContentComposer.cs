using JiraMetrics.Abstractions;
using JiraMetrics.Models;

using QuestPDF.Fluent;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Default PDF content composer for Jira analytics report.
/// </summary>
public sealed class PdfContentComposer : IPdfContentComposer
{
    private readonly IReadOnlyList<IPdfReportSection> _sections;

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
                new PdfTransitionAnalysisSection(),
                new PdfPathGroupsSection(),
                new PdfGeneralStatisticsSection(),
                new PdfFailuresSection()
            ])
    {
    }

    internal PdfContentComposer(IReadOnlyList<IPdfReportSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _sections = sections;
    }

    /// <inheritdoc />
    public void ComposeContent(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        column.Spacing(10);

        foreach (var section in _sections)
        {
            section.Compose(column, reportData);
        }
    }
}
