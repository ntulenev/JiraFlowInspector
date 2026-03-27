using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreFailuresSection
{
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold red]Failed issues[/]");

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Issue[/]")
            .AddColumn("[bold]Reason[/]");

        for (var i = 0; i < failures.Count; i++)
        {
            var failure = failures[i];
            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(failure.IssueKey.Value),
                Markup.Escape(failure.Reason.Value));
        }

        AnsiConsole.Write(table);
    }
}
