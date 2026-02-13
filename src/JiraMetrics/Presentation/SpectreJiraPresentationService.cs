using System.Globalization;
using System.Text;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

/// <summary>
/// Spectre.Console-based presentation service.
/// </summary>
public sealed class SpectreJiraPresentationService : IJiraPresentationService
{
    private readonly IJiraAnalyticsService _analyticsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreJiraPresentationService"/> class.
    /// </summary>
    /// <param name="analyticsService">Analytics service.</param>
    public SpectreJiraPresentationService(IJiraAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
    }

    /// <inheritdoc />
    public void ShowAuthenticationStarted() => AnsiConsole.MarkupLine("[grey]Authenticating with Jira...[/]");

    /// <inheritdoc />
    public void ShowAuthenticationSucceeded(JiraAuthUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        AnsiConsole.MarkupLine($"[green]Auth succeeded for user:[/] {Markup.Escape(user.DisplayName.Value)}");
    }

    /// <inheritdoc />
    public void ShowAuthenticationFailed(ErrorMessage errorMessage) => AnsiConsole.MarkupLine($"[red]Auth failed:[/] {Markup.Escape(errorMessage.Value)}");

    /// <inheritdoc />
    public void ShowIssueSearchFailed(ErrorMessage errorMessage) => AnsiConsole.MarkupLine($"[red]Failed to load issues:[/] {Markup.Escape(errorMessage.Value)}");

    /// <inheritdoc />
    public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AnsiConsole.Write(
            new Rule($"[bold cyan]Jira Transition Analytics[/] - [bold]{issueCount.Value} issue(s)[/]")
                .RuleStyle("grey")
                .LeftJustified());

        AnsiConsole.MarkupLine(
            $"[grey]Filter:[/] project = {Markup.Escape(settings.ProjectKey.Value)}, moved to {Markup.Escape(settings.DoneStatusName.Value)} in {Markup.Escape(settings.MonthLabel.Value)}");
        AnsiConsole.MarkupLine($"[grey]Required stage in path:[/] {Markup.Escape(settings.RequiredPathStage.Value)}");
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedFilter() => AnsiConsole.MarkupLine("[yellow]No issues matched this filter.[/]");

    /// <inheritdoc />
    public void ShowIssueLoaded(IssueKey issueKey) => AnsiConsole.MarkupLine($"[green]Loaded[/] {Markup.Escape(issueKey.Value)}");

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey) => AnsiConsole.MarkupLine($"[red]Failed[/] {Markup.Escape(issueKey.Value)}");

    /// <inheritdoc />
    public void ShowSpacer() => AnsiConsole.WriteLine();

    /// <inheritdoc />
    public void ShowNoIssuesLoaded() => AnsiConsole.MarkupLine("[red]No issues were loaded successfully.[/]");

    /// <inheritdoc />
    public void ShowNoIssuesMatchedRequiredStage() => AnsiConsole.MarkupLine("[yellow]No issues matched the required stage in path.[/]");

    /// <inheritdoc />
    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (issues.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Issues moved to Done this month[/]");

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Issue[/]")
            .AddColumn("[bold]Summary[/]")
            .AddColumn("[bold]Last Done At[/]");

        foreach (var issue in issues.OrderBy(x => x.Key.Value, StringComparer.OrdinalIgnoreCase))
        {
            var lastDoneAt = issue.Transitions
                .Where(x => string.Equals(x.To.Value, doneStatusName.Value, StringComparison.OrdinalIgnoreCase))
                .Select(x => (DateTimeOffset?)x.At)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var lastDoneAtText = lastDoneAt.HasValue
                ? lastDoneAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "-";

            _ = table.AddRow(
                Markup.Escape(issue.Key.Value),
                Markup.Escape(_analyticsService.Truncate(issue.Summary, new TextLength(120)).Value),
                Markup.Escape(lastDoneAtText));
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        AnsiConsole.MarkupLine(
            $"[grey]Successful:[/] {summary.SuccessfulCount.Value}    [grey]Matched stage:[/] {summary.MatchedStageCount.Value}    [grey]Failed:[/] {summary.FailedCount.Value}    [grey]Path groups:[/] {summary.PathGroupCount.Value}");
    }

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];

            AnsiConsole.Write(
                new Rule($"[bold]Group {i + 1}[/] - [bold]{group.Issues.Count} issue(s)[/]")
                    .RuleStyle("grey")
                    .LeftJustified());
            AnsiConsole.MarkupLine($"[grey]Path:[/] {Markup.Escape(group.PathLabel.Value)}");
            AnsiConsole.MarkupLine($"[grey]Issues:[/] {Markup.Escape(string.Join(", ", group.Issues.Select(x => x.Key.Value)))}");
            AnsiConsole.WriteLine();

            if (group.P75Transitions.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No transitions in this path.[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            AnsiConsole.MarkupLine("[bold]P75 Timeline Diagram[/]");
            var segments = group.P75Transitions
                .Select(x => (label: $"{x.From.Value} -> {x.To.Value}", duration: x.P75Duration))
                .ToList();
            RenderDurationTimeline(segments);
            AnsiConsole.WriteLine();

            var table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Grey)
                .AddColumn("[bold]From[/]")
                .AddColumn("[bold]To[/]")
                .AddColumn("[bold]P75 Time[/]")
                .AddColumn("[bold]Samples[/]");

            foreach (var transition in group.P75Transitions)
            {
                _ = table.AddRow(
                    Markup.Escape(transition.From.Value),
                    Markup.Escape(transition.To.Value),
                    _analyticsService.FormatDuration(transition.P75Duration).Value,
                    group.Issues.Count.ToString(CultureInfo.InvariantCulture));
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        if (failures.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold red]Failed issues[/]");

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Issue[/]")
            .AddColumn("[bold]Reason[/]");

        foreach (var failure in failures)
        {
            _ = table.AddRow(Markup.Escape(failure.IssueKey.Value), Markup.Escape(failure.Reason.Value));
        }

        AnsiConsole.Write(table);
    }

    private void RenderDurationTimeline(List<(string label, TimeSpan duration)> segments)
    {
        if (segments.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No transitions to render.[/]");
            return;
        }

        var normalized = segments
            .Select(segment => (segment.label, duration: segment.duration < TimeSpan.Zero ? TimeSpan.Zero : segment.duration))
            .ToList();

        var width = Math.Clamp(AnsiConsole.Profile.Width - 16, 30, 100);
        var total = normalized.Aggregate(TimeSpan.Zero, (acc, segment) => acc + segment.duration);
        var totalSeconds = Math.Max(0.0, total.TotalSeconds);

        var bar = new StringBuilder();
        var cursor = 0;
        var cumulativeSeconds = 0.0;

        for (var i = 0; i < normalized.Count; i++)
        {
            var (_, duration) = normalized[i];
            var startPos = totalSeconds <= 0
                ? 0
                : (int)Math.Round(cumulativeSeconds / totalSeconds * (width - 1));
            var endCumulativeSeconds = cumulativeSeconds + duration.TotalSeconds;
            var endPos = totalSeconds <= 0
                ? width - 1
                : (int)Math.Round(endCumulativeSeconds / totalSeconds * (width - 1));

            if (startPos < cursor)
            {
                startPos = cursor;
            }

            if (endPos < startPos)
            {
                endPos = startPos;
            }

            if (i == normalized.Count - 1)
            {
                endPos = width - 1;
            }

            if (startPos > cursor)
            {
                _ = bar.Append(new string(' ', startPos - cursor));
            }

            var segmentWidth = Math.Max(1, endPos - startPos + 1);
            var maxWidth = width - startPos;
            if (maxWidth <= 0)
            {
                break;
            }

            if (segmentWidth > maxWidth)
            {
                segmentWidth = maxWidth;
            }

            _ = bar.Append('[')
                .Append(GetTimelineColor(i))
                .Append(']')
                .Append(new string('#', segmentWidth))
                .Append("[/]");
            cursor = startPos + segmentWidth;
            cumulativeSeconds = endCumulativeSeconds;
        }

        if (cursor < width)
        {
            _ = bar.Append(new string(' ', width - cursor));
        }

        AnsiConsole.MarkupLine(bar.ToString());

        var left = "0";
        var right = $"P75 total: {_analyticsService.FormatDuration(total).Value}";
        var spacing = Math.Max(1, width - left.Length - right.Length);
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(left)}{new string(' ', spacing)}{Markup.Escape(right)}[/]");

        var legend = string.Join(
            "  ",
            normalized.Select((segment, i) => $"[{GetTimelineColor(i)}]#[/] {Markup.Escape(segment.label)}"));
        AnsiConsole.MarkupLine(legend);
    }

    private static string GetTimelineColor(int index) => index switch
    {
        0 => "deepskyblue1",
        1 => "dodgerblue1",
        2 => "cyan1",
        3 => "springgreen1",
        4 => "yellow1",
        5 => "orange1",
        _ => "grey82"
    };
}
