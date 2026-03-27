using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfArchTasksSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Settings.ArchTasksReport is not { } archTasksReport)
        {
            return;
        }

        _ = column.Item().Text("Architecture tasks report").Bold().FontSize(12);
        _ = column.Item().Text($"JQL: {archTasksReport.Jql}").FontColor(Colors.Grey.Darken1);

        var resolvedCount = reportData.ArchTasks.Count(static task => task.IsResolved);
        var openCount = reportData.ArchTasks.Count - resolvedCount;

        if (reportData.ArchTasks.Count == 0)
        {
            _ = column.Item().Text("No architecture tasks found for configured query.").FontColor(Colors.Grey.Darken1);
            _ = column.Item().Text("Total tasks: 0    Resolved: 0    Open: 0").FontColor(Colors.Grey.Darken1);
            return;
        }

        var now = DateTimeOffset.Now;
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(78);
                columns.ConstantColumn(92);
                columns.ConstantColumn(92);
                columns.ConstantColumn(70);
                columns.RelativeColumn(3.8f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Resolved At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Days in work");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < reportData.ArchTasks.Count; i++)
            {
                var task = reportData.ArchTasks[i];
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, task.Key);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(task.Key.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.ResolvedAt.HasValue
                        ? task.ResolvedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                        : "-");
                table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(text =>
                    {
                        var span = text.Span(PdfPresentationFormatting.FormatCalendarDayDurationValue(task.GetElapsed(now)));
                        _ = span.FontColor(task.IsResolved ? Colors.Black : Colors.Red.Darken2);
                    });
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.Title.Truncate(new TextLength(140)).Value);
            }
        });

        _ = column
            .Item()
            .Text($"Total tasks: {reportData.ArchTasks.Count}    Resolved: {resolvedCount}    Open: {openCount}")
            .FontColor(Colors.Grey.Darken1);
    }
}
