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
        if (reportData.Settings.TestCoverage is not { Enabled: true } settings
            || !reportData.IsOptionalSectionAvailable(OptionalReportSection.TestCoverage))
        {
            return;
        }

        var presentationData = TestCoveragePresentationData.Create(
            settings,
            reportData.Ratios.TestCoverage);
        _ = column.Item().Text("Automated test coverage").Bold().FontSize(12);
        _ = column.Item().Text(
            $"Issue types: {presentationData.IssueTypesLabel}    Test project: {presentationData.TestProjectLabel}    Link: {presentationData.LinkLabel}")
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

            AddTextRow(table, "Done in selected period", presentationData.TotalIssues.Value.ToString(CultureInfo.InvariantCulture));
            AddTextRow(table, "Covered by automated tests", presentationData.CoveredIssueCount.Value.ToString(CultureInfo.InvariantCulture));
            AddTextRow(table, "Coverage", presentationData.CoverageText);
        });
    }

    private static void AddTextRow(TableDescriptor table, string label, string value)
    {
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(label);
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(value);
    }
}
