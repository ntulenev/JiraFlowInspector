using System.Globalization;

using JiraMetrics.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders the failed issue loading section of the PDF report.
/// </summary>
internal sealed class PdfFailuresSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Failures.Count == 0)
        {
            return;
        }

        _ = column.Item().Text("Failed issues").Bold().FontSize(12).FontColor(Colors.Red.Darken2);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(86);
                columns.RelativeColumn(4);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Reason");
            });

            for (var i = 0; i < reportData.Failures.Count; i++)
            {
                var failure = reportData.Failures[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, failure.IssueKey);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(failure.IssueKey.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(failure.Reason.Value);
            }
        });
    }
}
