using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreGlobalIncidentsSection
{

    public SpectreGlobalIncidentsSection(bool showTimeCalculationsInHoursOnly)
    {
        _showTimeCalculationsInHoursOnly = showTimeCalculationsInHoursOnly;
    }

    public void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents)
    {
        AnsiConsole.MarkupLine("[bold]Global incidents report[/]");
        AnsiConsole.MarkupLine(
            $"[grey]Namespace:[/] {Markup.Escape(settings.Namespace)}    [grey]Period:[/] {Markup.Escape(reportPeriod.Label)}");
        if (!string.IsNullOrWhiteSpace(settings.JqlFilter))
        {
            AnsiConsole.MarkupLine($"[grey]JQL filter:[/] {Markup.Escape(settings.JqlFilter)}");
        }
        else if (!string.IsNullOrWhiteSpace(settings.SearchPhrase))
        {
            AnsiConsole.MarkupLine($"[grey]Search phrase:[/] {Markup.Escape(settings.SearchPhrase)}");
        }

        if (settings.AdditionalFieldNames.Count > 0)
        {
            var additionalFields = string.Join(", ", settings.AdditionalFieldNames);
            AnsiConsole.MarkupLine($"[grey]Additional fields:[/] {Markup.Escape(additionalFields)}");
        }

        if (incidents.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No incidents found for selected period.[/]");
            return;
        }

        var includeAdditionalFields = settings.AdditionalFieldNames.Count > 0;
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Jira ID[/]")
            .AddColumn("[bold]Title[/]")
            .AddColumn("[bold]Incident Start UTC[/]")
            .AddColumn("[bold]Incident Recovery UTC[/]")
            .AddColumn("[bold]Duration[/]")
            .AddColumn("[bold]Impact[/]")
            .AddColumn("[bold]Urgency[/]");

        if (includeAdditionalFields)
        {
            _ = table.AddColumn("[bold]Additional fields[/]");
        }

        var orderedIncidents = incidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIncidents.Count; i++)
        {
            var incident = orderedIncidents[i];
            var row = new List<string>
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(incident.Key.Value),
                Markup.Escape(incident.Title.Truncate(new TextLength(120)).Value),
                Markup.Escape(SpectrePresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentStartUtc)),
                Markup.Escape(SpectrePresentationFormatting.FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc)),
                Markup.Escape(SpectrePresentationFormatting.FormatIncidentDuration(incident.Duration, _showTimeCalculationsInHoursOnly)),
                Markup.Escape(incident.Impact ?? "-"),
                Markup.Escape(incident.Urgency ?? "-")
            };

            if (includeAdditionalFields)
            {
                var additionalFields = incident.AdditionalFields.Count == 0
                    ? "-"
                    : string.Join(
                        Environment.NewLine,
                        incident.AdditionalFields.Select(static pair => $"{pair.Key}: {pair.Value}"));
                row.Add(Markup.Escape(additionalFields));
            }

            _ = table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);
        var totalDuration = SpectrePresentationFormatting.SumIncidentDurations(orderedIncidents);
        AnsiConsole.MarkupLine($"[grey]Total duration:[/] {Markup.Escape(SpectrePresentationFormatting.FormatIncidentDuration(totalDuration, _showTimeCalculationsInHoursOnly))}");
    }
    private readonly bool _showTimeCalculationsInHoursOnly;
}
