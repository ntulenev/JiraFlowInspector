using System.Globalization;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the global-incidents HTML section.
/// </summary>
internal sealed class HtmlGlobalIncidentsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        if (reportData.Settings.GlobalIncidentsReport is null)
        {
            return string.Empty;
        }

        var rows = reportData.Source.GlobalIncidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((incident, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(incident.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, incident.Key)),
                BuildTextCell(PresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentStartUtc), incident.IncidentStartUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc), incident.IncidentRecoveryUtc?.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatIncidentDuration(incident.Duration, reportData.Settings.ShowTimeCalculationsInHoursOnly), incident.Duration?.TotalMinutes),
                BuildTextCell(incident.Impact ?? "-"),
                BuildTextCell(incident.Urgency ?? "-"),
                BuildTextCell(incident.Title.Value)
            ]))
            .ToList();

        return BuildTableSection(
            "global-incidents",
            "Global Incidents",
            "No incidents found.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Started UTC", "number", "Started UTC"),
                new TableColumn("Recovered UTC", "number", "Recovered UTC"),
                new TableColumn("Duration", "number", "Duration"),
                new TableColumn("Impact", "text", "Impact"),
                new TableColumn("Urgency", "text", "Urgency"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 2);
    }
}
