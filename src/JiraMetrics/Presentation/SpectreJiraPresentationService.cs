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
        if (settings.CreatedAfter is { } createdAfter)
        {
            AnsiConsole.MarkupLine($"[grey]Created after:[/] {Markup.Escape(createdAfter.ToString())}");
        }

        if (settings.IssueTypes.Count > 0)
        {
            var issueTypes = string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value));
            AnsiConsole.MarkupLine($"[grey]Issue types:[/] {Markup.Escape(issueTypes)}");
        }

        if (!string.IsNullOrWhiteSpace(settings.CustomFieldName)
            && !string.IsNullOrWhiteSpace(settings.CustomFieldValue))
        {
            AnsiConsole.MarkupLine(
                $"[grey]Custom field filter:[/] {Markup.Escape(settings.CustomFieldName)} = {Markup.Escape(settings.CustomFieldValue)}");
        }

        var requiredStages = settings.RequiredPathStages.Count == 0
            ? "-"
            : string.Join(", ", settings.RequiredPathStages.Select(static stage => stage.Value));
        AnsiConsole.MarkupLine($"[grey]Required stages in path:[/] {Markup.Escape(requiredStages)}");
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
    public void ShowNoIssuesMatchedRequiredStage() => AnsiConsole.MarkupLine("[yellow]No issues matched the required stages in path.[/]");

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
            .AddColumn("[bold]Type[/]")
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
                Markup.Escape(issue.IssueType.Value),
                Markup.Escape(issue.Summary.Truncate(new TextLength(120)).Value),
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

            AnsiConsole.MarkupLine("[bold]Timeline Diagram[/]");
            var stageDurations = group.P75Transitions
                .Select(static transition => (stage: transition.From.Value, duration: transition.P75Duration))
                .ToList();
            var totalTtm = DurationLabel.FromDuration(group.TotalP75).Value;
            RenderDurationTimeline(stageDurations, totalTtm);
            AnsiConsole.WriteLine();

            var table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Grey)
                .AddColumn("[bold]From[/]")
                .AddColumn("[bold]To[/]")
                .AddColumn("[bold]P75 Time[/]");

            foreach (var transition in group.P75Transitions)
            {
                _ = table.AddRow(
                    Markup.Escape(transition.From.Value),
                    Markup.Escape(transition.To.Value),
                    DurationLabel.FromDuration(transition.P75Duration).Value);
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

    private static void RenderDurationTimeline(
        List<(string stage, TimeSpan duration)> stageDurations,
        string totalTtm)
    {
        if (stageDurations.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No transitions to render.[/]");
            return;
        }

        var normalizedDurations = stageDurations
            .Select(static stageDuration => (stageDuration.stage, duration: stageDuration.duration < TimeSpan.Zero ? TimeSpan.Zero : stageDuration.duration))
            .ToList();

        var width = Math.Clamp(AnsiConsole.Profile.Width - 4, 30, 110);
        var stageColors = BuildStageColors(normalizedDurations);
        var segmentWidths = ComputeSegmentWidths(normalizedDurations, width);
        var bar = new StringBuilder();

        for (var i = 0; i < normalizedDurations.Count; i++)
        {
            if (segmentWidths[i] <= 0)
            {
                continue;
            }

            _ = bar.Append('[')
                .Append(stageColors[normalizedDurations[i].stage])
                .Append(']')
                .Append(new string('█', segmentWidths[i]))
                .Append("[/]");
        }

        AnsiConsole.MarkupLine(bar.ToString());

        var legend = string.Join(
            "  ",
            stageColors.Select(static pair => $"[{pair.Value}]■[/] {Markup.Escape(pair.Key)}"));
        AnsiConsole.MarkupLine(legend);

        AnsiConsole.MarkupLine($"[grey]TTM 75P:[/] {Markup.Escape(totalTtm)}");
    }

    private static Dictionary<string, string> BuildStageColors(
        List<(string stage, TimeSpan duration)> stageDurations)
    {
        var orderedStages = new List<string>();
        foreach (var (stage, _) in stageDurations)
        {
            if (!orderedStages.Contains(stage, StringComparer.OrdinalIgnoreCase))
            {
                orderedStages.Add(stage);
            }
        }

        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < orderedStages.Count; i++)
        {
            colors[orderedStages[i]] = GetTimelineColor(i);
        }

        return colors;
    }

    private static List<int> ComputeSegmentWidths(
        List<(string stage, TimeSpan duration)> stageDurations,
        int width)
    {
        var segmentCount = stageDurations.Count;
        if (segmentCount == 0 || width <= 0)
        {
            return [];
        }

        var totalSeconds = stageDurations.Sum(static segment => Math.Max(0.0, segment.duration.TotalSeconds));
        if (totalSeconds <= 0)
        {
            var equalWidth = Math.Max(1, width / segmentCount);
            var equalWidths = Enumerable.Repeat(equalWidth, segmentCount).ToList();
            var remaining = width - equalWidths.Sum();
            for (var i = 0; i < remaining; i++)
            {
                equalWidths[i % segmentCount]++;
            }

            return equalWidths;
        }

        var rawWidths = stageDurations
            .Select(segment => Math.Max(0.0, segment.duration.TotalSeconds) / totalSeconds * width)
            .ToList();
        var widths = rawWidths.Select(static rawWidth => (int)Math.Floor(rawWidth)).ToList();

        var remainder = width - widths.Sum();
        var order = rawWidths
            .Select((rawWidth, index) => (index, fraction: rawWidth - Math.Floor(rawWidth)))
            .OrderByDescending(static item => item.fraction)
            .ToList();

        for (var i = 0; i < remainder; i++)
        {
            widths[order[i % order.Count].index]++;
        }

        return widths;
    }

    private static string GetTimelineColor(int index) => index switch
    {
        0 => "deepskyblue1",
        1 => "dodgerblue1",
        2 => "cyan1",
        3 => "springgreen1",
        4 => "yellow1",
        5 => "orange1",
        6 => "gold1",
        _ => "grey82"
    };
}
