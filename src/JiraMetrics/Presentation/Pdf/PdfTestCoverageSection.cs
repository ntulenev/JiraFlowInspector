using System.Globalization;

using JiraMetrics.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders automated test coverage metrics in the PDF report.
/// </summary>
internal sealed class PdfTestCoverageSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraReportData reportData)
    {
        if (reportData.Settings.TestCoverage is not { Enabled: true } settings)
        {
            return;
        }

        _ = column.Item().Text("Automated test coverage").Bold().FontSize(12);
        _ = column.Item().Text(
            $"Issue types: {string.Join(", ", settings.IssueTypes.Select(static x => x.Value))}    Test project: {settings.TestProjectKey.Value}    Link: {settings.LinkName}")
            .FontColor(Colors.Grey.Darken1);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Metric");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Value");
            });

            AddTextRow(table, "Done in selected period", reportData.TestCoverage.TotalIssues.Value.ToString(CultureInfo.InvariantCulture));
            AddTextRow(table, "Covered by automated tests", reportData.TestCoverage.CoveredIssueCount.Value.ToString(CultureInfo.InvariantCulture));
            AddTextRow(table, "Coverage", PresentationFormatting.FormatPercentage(reportData.TestCoverage.CoveragePercentage));
        });
    }

    private static void AddTextRow(TableDescriptor table, string label, string value)
    {
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(label);
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(value);
    }
}
