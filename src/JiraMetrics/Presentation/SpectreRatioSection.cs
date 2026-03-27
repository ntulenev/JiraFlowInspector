using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreRatioSection
{
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        AnsiConsole.MarkupLine("[bold]All tasks ratio[/]");
        if (!string.IsNullOrWhiteSpace(customFieldName)
            && !string.IsNullOrWhiteSpace(customFieldValue))
        {
            AnsiConsole.MarkupLine(
                $"[grey]Filtered by:[/] {Markup.Escape(customFieldName)} = {Markup.Escape(customFieldValue)}");
        }

        AnsiConsole.Write(CreateRatioSummaryTable(
            "Issue types",
            "All",
            snapshot.CreatedThisMonth,
            snapshot.OpenThisMonth,
            snapshot.MovedToDoneThisMonth,
            snapshot.RejectedThisMonth,
            snapshot.FinishedThisMonth));
    }

    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        AnsiConsole.MarkupLine(
            $"[green]All tasks ratio data loaded:[/] created = {snapshot.CreatedThisMonth.Value}, done = {snapshot.MovedToDoneThisMonth.Value}, rejected = {snapshot.RejectedThisMonth.Value}, finished = {snapshot.FinishedThisMonth.Value}");
    }

    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        if (bugIssueNames.Count == 0)
        {
            return;
        }

        var bugTypes = string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));

        AnsiConsole.MarkupLine("[bold]Bug ratio[/]");
        if (!string.IsNullOrWhiteSpace(customFieldName)
            && !string.IsNullOrWhiteSpace(customFieldValue))
        {
            AnsiConsole.MarkupLine(
                $"[grey]Filtered by:[/] {Markup.Escape(customFieldName)} = {Markup.Escape(customFieldValue)}");
        }

        AnsiConsole.Write(CreateRatioSummaryTable(
            "Bug issue types",
            bugTypes,
            snapshot.CreatedThisMonth,
            snapshot.OpenThisMonth,
            snapshot.MovedToDoneThisMonth,
            snapshot.RejectedThisMonth,
            snapshot.FinishedThisMonth));

        if (snapshot.OpenIssues.Count == 0 && snapshot.DoneIssues.Count == 0 && snapshot.RejectedIssues.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Bug ratio details[/]");
        AnsiConsole.MarkupLine("[bold red]Open issues[/]");
        RenderBugIssueDetailsTable(snapshot.OpenIssues, "red", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Done issues[/]");
        RenderBugIssueDetailsTable(snapshot.DoneIssues, "green", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold orange1]Rejected issues[/]");
        RenderBugIssueDetailsTable(snapshot.RejectedIssues, "orange1", includeCreationDate: false);
    }

    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        AnsiConsole.MarkupLine(
            $"[green]Bug ratio data loaded:[/] created = {snapshot.CreatedThisMonth.Value}, done = {snapshot.MovedToDoneThisMonth.Value}, rejected = {snapshot.RejectedThisMonth.Value}, finished = {snapshot.FinishedThisMonth.Value}");
    }

    private static Table CreateRatioSummaryTable(
        string scopeLabel,
        string scopeValue,
        ItemCount createdThisMonth,
        ItemCount openThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow(Markup.Escape(scopeLabel), Markup.Escape(scopeValue));
        _ = table.AddRow("[red]Open in selected period[/]", $"[red]{openThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[green]Done in selected period[/]", $"[green]{movedToDoneThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[orange1]Rejected in selected period[/]", $"[orange1]{rejectedThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[deepskyblue1]Finished in selected period[/]", $"[deepskyblue1]{finishedThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("Finished / Created", BuildFinishedToCreatedRatioText(createdThisMonth, finishedThisMonth));

        return table;
    }

    private static string BuildFinishedToCreatedRatioText(ItemCount createdThisMonth, ItemCount finishedThisMonth) =>
        createdThisMonth.Value == 0
            ? "n/a"
            : $"{finishedThisMonth.Value * 100.0 / createdThisMonth.Value:0.##}%";

    private static void RenderBugIssueDetailsTable(
        IReadOnlyList<IssueListItem> issues,
        string titleColor,
        bool includeCreationDate)
    {
        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No issues.[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Jira ID[/]");

        if (includeCreationDate)
        {
            _ = table.AddColumn("[bold]Creation Date[/]");
        }

        _ = table.AddColumn("[bold]Title[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            var row = new List<string>
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issue.Key.Value)
            };
            if (includeCreationDate)
            {
                var createdAtText = issue.CreatedAt.HasValue
                    ? issue.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : "-";
                row.Add(Markup.Escape(createdAtText));
            }

            row.Add($"[{titleColor}]{Markup.Escape(issue.Title.Truncate(new TextLength(120)).Value)}[/]");
            _ = table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);
    }
}
