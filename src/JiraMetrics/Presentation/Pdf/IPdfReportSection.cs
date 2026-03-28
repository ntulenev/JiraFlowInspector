using JiraMetrics.Models;

using QuestPDF.Fluent;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Represents one composable section of the generated PDF report.
/// </summary>
internal interface IPdfReportSection
{
    /// <summary>
    /// Composes the section content into the target PDF column.
    /// </summary>
    /// <param name="column">Target QuestPDF column descriptor.</param>
    /// <param name="reportData">Aggregated report data.</param>
    void Compose(ColumnDescriptor column, JiraPdfReportData reportData);
}
