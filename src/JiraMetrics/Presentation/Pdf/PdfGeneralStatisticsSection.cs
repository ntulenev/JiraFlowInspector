using System.Globalization;

using JiraMetrics.Models;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfGeneralStatisticsSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.Settings.ShowGeneralStatistics)
        {
            return;
        }

        _ = column.Item().Text("General statistics").Bold().FontSize(12);
        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        _ = column.Item().Text("Data as of: " + generatedAt).FontColor(Colors.Grey.Darken1);
        _ = column.Item().Text("Scope: all not finished tasks").FontColor(Colors.Grey.Darken1);

        var excludedStatuses = reportData.Settings.RejectStatusName is { } rejectStatus
            ? $"{reportData.Settings.DoneStatusName.Value}, {rejectStatus.Value}"
            : reportData.Settings.DoneStatusName.Value;
        _ = column.Item().Text("Statuses excluded: " + excludedStatuses).FontColor(Colors.Grey.Darken1);

        if (reportData.OpenIssuesByStatus.Count == 0)
        {
            _ = column.Item().Text("No issues outside excluded statuses.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedStatuses = reportData.OpenIssuesByStatus
            .OrderByDescending(static summary => summary.Count.Value)
            .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(2.8f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Status");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Breakdown by type");
            });

            foreach (var statusSummary in orderedStatuses)
            {
                var issueTypeBreakdown = statusSummary.IssueTypes.Count == 0
                    ? "-"
                    : string.Join(
                        Environment.NewLine,
                        statusSummary.IssueTypes
                            .OrderByDescending(static summary => summary.Count.Value)
                            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                            .Select(summary =>
                                $"{summary.IssueType.Value} - {summary.Count.Value.ToString(CultureInfo.InvariantCulture)}"));

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(statusSummary.Status.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(statusSummary.Count.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issueTypeBreakdown);
            }
        });
    }
}
