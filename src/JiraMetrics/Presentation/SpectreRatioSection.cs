using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

/// <summary>
/// Renders issue-ratio metrics in the console.
/// </summary>
internal sealed class SpectreRatioSection
{
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        var presentationData = IssueRatioPresentationData.Create(snapshot);
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
            presentationData));
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
        var presentationData = IssueRatioPresentationData.Create(snapshot);

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
            presentationData));

        if (!presentationData.HasIssueDetails)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Bug ratio details[/]");
        AnsiConsole.MarkupLine("[bold red]Open issues[/]");
        RenderBugIssueDetailsTable(presentationData.OpenIssues, "red", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Done issues[/]");
        RenderBugIssueDetailsTable(presentationData.DoneIssues, "green", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold orange1]Rejected issues[/]");
        RenderBugIssueDetailsTable(presentationData.RejectedIssues, "orange1", includeCreationDate: false);
    }

    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        AnsiConsole.MarkupLine(
            $"[green]Bug ratio data loaded:[/] created = {snapshot.CreatedThisMonth.Value}, done = {snapshot.MovedToDoneThisMonth.Value}, rejected = {snapshot.RejectedThisMonth.Value}, finished = {snapshot.FinishedThisMonth.Value}");
    }

    public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);

        var presentationData = TestCoveragePresentationData.Create(settings, snapshot);

        AnsiConsole.MarkupLine("[bold]Automated test coverage[/]");
        AnsiConsole.MarkupLine($"[grey]Issue types:[/] {Markup.Escape(presentationData.IssueTypesLabel)}");
        AnsiConsole.MarkupLine($"[grey]Test project:[/] {Markup.Escape(presentationData.TestProjectLabel)}");
        AnsiConsole.MarkupLine($"[grey]Link:[/] {Markup.Escape(presentationData.LinkLabel)}");

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow("Done in selected period", presentationData.TotalIssues.Value.ToString(CultureInfo.InvariantCulture));
        _ = table.AddRow("Covered by automated tests", presentationData.CoveredIssueCount.Value.ToString(CultureInfo.InvariantCulture));
        _ = table.AddRow("Coverage", presentationData.CoverageText);
        AnsiConsole.Write(table);
    }

    private static Table CreateRatioSummaryTable(
        string scopeLabel,
        string scopeValue,
        IssueRatioPresentationData ratio)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow(Markup.Escape(scopeLabel), Markup.Escape(scopeValue));
        _ = table.AddRow("[red]Open in selected period[/]", $"[red]{ratio.OpenCount.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[green]Done in selected period[/]", $"[green]{ratio.DoneCount.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[orange1]Rejected in selected period[/]", $"[orange1]{ratio.RejectedCount.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[deepskyblue1]Finished in selected period[/]", $"[deepskyblue1]{ratio.FinishedCount.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("Finished / Created", ratio.FinishedToCreatedRatioText);

        return table;
    }

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

        for (var i = 0; i < issues.Count; i++)
        {
            var issue = issues[i];
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
