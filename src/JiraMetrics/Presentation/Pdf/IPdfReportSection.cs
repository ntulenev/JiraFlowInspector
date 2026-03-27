using JiraMetrics.Models;

using QuestPDF.Fluent;

namespace JiraMetrics.Presentation.Pdf;

internal interface IPdfReportSection
{
    void Compose(ColumnDescriptor column, JiraPdfReportData reportData);
}
