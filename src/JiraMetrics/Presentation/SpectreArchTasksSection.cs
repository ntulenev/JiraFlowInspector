using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreArchTasksSection
{
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        AnsiConsole.MarkupLine("[bold]Architecture tasks report[/]");
        AnsiConsole.MarkupLine($"[grey]JQL:[/] {Markup.Escape(settings.Jql)}");

        var resolvedCount = tasks.Count(static task => task.IsResolved);
        var openCount = tasks.Count - resolvedCount;

        if (tasks.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No architecture tasks found for configured query.[/]");
            AnsiConsole.MarkupLine(
                $"[grey]Total tasks:[/] 0    [grey]Resolved:[/] 0    [grey]Open:[/] 0");
            return;
        }

        var now = DateTimeOffset.Now;
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Jira ID[/]")
            .AddColumn("[bold]Created At[/]")
            .AddColumn("[bold]Resolved At[/]")
            .AddColumn("[bold]Days in work[/]")
            .AddColumn("[bold]Title[/]");

        for (var i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var createdAtText = task.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var resolvedAtText = task.ResolvedAt.HasValue
                ? task.ResolvedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "-";
            var daysInWorkText = SpectrePresentationFormatting.FormatCalendarDayDurationValue(task.GetElapsed(now));
            var daysInWorkMarkup = task.IsResolved
                ? Markup.Escape(daysInWorkText)
                : $"[red]{Markup.Escape(daysInWorkText)}[/]";

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(task.Key.Value),
                Markup.Escape(createdAtText),
                Markup.Escape(resolvedAtText),
                daysInWorkMarkup,
                Markup.Escape(task.Title.Truncate(new TextLength(120)).Value));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(
            $"[grey]Total tasks:[/] {tasks.Count}    [grey]Resolved:[/] {resolvedCount}    [grey]Open:[/] {openCount}");
    }
}
