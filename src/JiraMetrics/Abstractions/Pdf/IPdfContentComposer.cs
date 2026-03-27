using JiraMetrics.Models;

using QuestPDF.Fluent;

namespace JiraMetrics.Abstractions.Pdf;

/// <summary>
/// Composes PDF content body for Jira analytics report.
/// </summary>
public interface IPdfContentComposer
{
    /// <summary>
    /// Composes content section for PDF report.
    /// </summary>
    /// <param name="column">QuestPDF column descriptor.</param>
    /// <param name="reportData">Aggregated report data.</param>
    void ComposeContent(ColumnDescriptor column, JiraPdfReportData reportData);
}

