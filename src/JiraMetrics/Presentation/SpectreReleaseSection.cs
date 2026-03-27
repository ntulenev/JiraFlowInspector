using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreReleaseSection
{
    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        AnsiConsole.MarkupLine("[bold]Release report[/]");
        AnsiConsole.MarkupLine($"[bold red]All releases by label \"{Markup.Escape(settings.ProjectLabel)}\"[/]");
        AnsiConsole.MarkupLine(
            $"[grey]Project:[/] {Markup.Escape(settings.ReleaseProjectKey.Value)}    [grey]Label:[/] {Markup.Escape(settings.ProjectLabel)}    [grey]Period:[/] {Markup.Escape(reportPeriod.Label)}");
        if (!string.IsNullOrWhiteSpace(settings.ComponentsFieldName))
        {
            AnsiConsole.MarkupLine($"[grey]Components field:[/] {Markup.Escape(settings.ComponentsFieldName)}");
        }
        if (!string.IsNullOrWhiteSpace(settings.EnvironmentFieldName))
        {
            AnsiConsole.MarkupLine("[grey]Environments:[/] shown per release");
        }
        AnsiConsole.MarkupLine("[grey]Hot-fix markers:[/]");
        foreach (var (fieldName, values) in settings.HotFixRules
                     .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var valuesText = string.Join(", ", values);
            AnsiConsole.MarkupLine($"[grey]-[/] {Markup.Escape(fieldName)} = {Markup.Escape(valuesText)}");
        }

        var totalReleases = releases.Count;
        var hotFixCount = releases.Count(static release => release.IsHotFix);
        var rollbackCount = releases.Count(static release => !string.IsNullOrWhiteSpace(release.RollbackType));

        if (releases.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No releases found for selected period.[/]");
            AnsiConsole.MarkupLine(
                $"[grey]Total releases:[/] {totalReleases}    [grey]Hotfix count:[/] {hotFixCount}    [grey]Rollbacks count:[/] {rollbackCount}");
            return;
        }

        var includeComponents = !string.IsNullOrWhiteSpace(settings.ComponentsFieldName);
        var includeEnvironments = !string.IsNullOrWhiteSpace(settings.EnvironmentFieldName);

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Release Date[/]")
            .AddColumn("[bold]Jira ID[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Tasks[/]");

        if (includeComponents)
        {
            _ = table.AddColumn("[bold]Components[/]");
        }

        if (includeEnvironments)
        {
            _ = table.AddColumn("[bold]Environments[/]");
        }

        _ = table.AddColumn(new TableColumn("[bold]Rollback type[/]").NoWrap());
        _ = table.AddColumn("[bold]Title[/]");

        var orderedReleases = releases
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedReleases.Count; i++)
        {
            var release = orderedReleases[i];
            var tasksText = release.Tasks == 0
                ? "-"
                : release.Tasks.ToString(CultureInfo.InvariantCulture);
            var row = new List<string>
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                SpectrePresentationFormatting.FormatReleaseCell(release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), release.IsHotFix),
                SpectrePresentationFormatting.FormatReleaseCell(release.Key.Value, release.IsHotFix),
                SpectrePresentationFormatting.FormatReleaseCell(release.Status.Value, release.IsHotFix),
                SpectrePresentationFormatting.FormatReleaseCell(tasksText, release.IsHotFix)
            };
            if (includeComponents)
            {
                var componentsText = release.Components == 0
                    ? "-"
                    : release.Components.ToString(CultureInfo.InvariantCulture);
                row.Add(SpectrePresentationFormatting.FormatReleaseCell(componentsText, release.IsHotFix));
            }
            if (includeEnvironments)
            {
                var environmentsText = release.EnvironmentNames.Count == 0
                    ? "-"
                    : string.Join(", ", release.EnvironmentNames);
                row.Add(SpectrePresentationFormatting.FormatReleaseCell(environmentsText, release.IsHotFix));
            }

            var rollbackText = string.IsNullOrWhiteSpace(release.RollbackType)
                ? "-"
                : release.RollbackType;
            row.Add(SpectrePresentationFormatting.FormatReleaseCell(rollbackText, release.IsHotFix));
            row.Add(SpectrePresentationFormatting.FormatReleaseCell(release.Title.Truncate(new TextLength(120)).Value, release.IsHotFix));
            _ = table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(
            $"[grey]Total releases:[/] {totalReleases}    [grey]Hotfix count:[/] {hotFixCount}    [grey]Rollbacks count:[/] {rollbackCount}");

        if (!includeComponents)
        {
            return;
        }

        var componentSummaries = SpectrePresentationFormatting.BuildComponentReleaseSummaries(orderedReleases);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Components release table[/]");
        if (componentSummaries.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No components data.[/]");
            return;
        }

        var componentsTable = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Component name[/]")
            .AddColumn("[bold]Release counts[/]");

        for (var i = 0; i < componentSummaries.Count; i++)
        {
            var (componentName, releaseCount) = componentSummaries[i];
            _ = componentsTable.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(componentName),
                releaseCount.ToString(CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(componentsTable);
    }
}
