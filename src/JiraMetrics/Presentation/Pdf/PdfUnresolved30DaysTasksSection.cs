using System.Globalization;

using JiraMetrics.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders unresolved tasks older than 30 days in the PDF report.
/// </summary>
internal sealed class PdfUnresolved30DaysTasksSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraReportData reportData)
    {
        if (reportData.Settings.Unresolved30DaysTasksReport is null)
        {
            return;
        }

        _ = column.Item().Text("Unresolved 30+ Days Tasks").Bold().FontSize(12);
        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        _ = column.Item()
            .Text($"Current snapshot as of {generatedAt}; this is not a historical period slice.")
            .FontColor(Colors.Grey.Darken1);

        if (reportData.Unresolved30DaysTasks.Count == 0)
        {
            _ = column.Item().Text("No unresolved tasks older than 30 days found.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedIssues = reportData.Unresolved30DaysTasks
            .OrderBy(static issue => issue.CreatedAt)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(3);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var index = 0; index < orderedIssues.Length; index++)
            {
                var issue = orderedIssues[index];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((index + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key))
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(issue.Key.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(issue.CreatedAt.HasValue
                        ? issue.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                        : "-");
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.Title.Value);
            }
        });
    }
}
