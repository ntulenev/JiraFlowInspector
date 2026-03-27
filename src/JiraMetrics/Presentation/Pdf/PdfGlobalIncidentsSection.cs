using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfGlobalIncidentsSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Settings.GlobalIncidentsReport is not { } globalIncidentsReport)
        {
            return;
        }

        _ = column.Item().Text("Global incidents report").Bold().FontSize(12);
        _ = column
            .Item()
            .Text($"Namespace: {globalIncidentsReport.Namespace}    Period: {reportData.Settings.ReportPeriod.Label}")
            .FontColor(Colors.Grey.Darken1);
        if (!string.IsNullOrWhiteSpace(globalIncidentsReport.JqlFilter))
        {
            _ = column.Item().Text($"JQL filter: {globalIncidentsReport.JqlFilter}").FontColor(Colors.Grey.Darken1);
        }
        else if (!string.IsNullOrWhiteSpace(globalIncidentsReport.SearchPhrase))
        {
            _ = column.Item().Text($"Search phrase: {globalIncidentsReport.SearchPhrase}").FontColor(Colors.Grey.Darken1);
        }

        if (globalIncidentsReport.AdditionalFieldNames.Count > 0)
        {
            _ = column
                .Item()
                .Text("Additional fields: " + string.Join(", ", globalIncidentsReport.AdditionalFieldNames))
                .FontColor(Colors.Grey.Darken1);
        }

        if (reportData.GlobalIncidents.Count == 0)
        {
            _ = column.Item().Text("No incidents found for selected period.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var includeAdditionalFields = globalIncidentsReport.AdditionalFieldNames.Count > 0;
        var orderedIncidents = reportData.GlobalIncidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(78);
                columns.RelativeColumn(3.6f);
                columns.ConstantColumn(92);
                columns.ConstantColumn(92);
                columns.ConstantColumn(68);
                columns.ConstantColumn(84);
                columns.ConstantColumn(72);
                if (includeAdditionalFields)
                {
                    columns.RelativeColumn(2.6f);
                }
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Incident Start UTC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Incident Recovery UTC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Duration");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Impact");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Urgency");
                if (includeAdditionalFields)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Additional fields");
                }
            });

            for (var i = 0; i < orderedIncidents.Length; i++)
            {
                var incident = orderedIncidents[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var incidentUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, incident.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(incidentUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(incident.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Title.Truncate(new TextLength(140)).Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(PdfPresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentStartUtc));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(PdfPresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(
                    PdfPresentationFormatting.FormatIncidentDuration(incident.Duration, reportData.Settings.ShowTimeCalculationsInHoursOnly));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Impact ?? "-");
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Urgency ?? "-");
                if (includeAdditionalFields)
                {
                    var additionalFields = incident.AdditionalFields.Count == 0
                        ? "-"
                        : string.Join(
                            Environment.NewLine,
                            incident.AdditionalFields.Select(static pair => $"{pair.Key}: {pair.Value}"));
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(additionalFields);
                }
            }
        });

        var totalDuration = PdfPresentationFormatting.SumIncidentDurations(orderedIncidents);
        _ = column
            .Item()
            .Text("Total duration: " + PdfPresentationFormatting.FormatIncidentDuration(totalDuration, reportData.Settings.ShowTimeCalculationsInHoursOnly))
            .FontColor(Colors.Grey.Darken1);
    }
}
