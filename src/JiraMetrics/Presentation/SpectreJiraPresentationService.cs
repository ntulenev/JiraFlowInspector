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
    public void ShowIssueLoadingStarted(ItemCount totalIssues)
    {
        _issueLoadTotal = totalIssues.Value;
        _issueLoadProcessed = 0;
        _issueLoadFailed = 0;
        _issueLoadStep = Math.Max(1, _issueLoadTotal / 10);

        AnsiConsole.MarkupLine($"[grey]Loading issue timelines:[/] 0/{_issueLoadTotal}");
    }

    /// <inheritdoc />
    public void ShowIssueLoaded(IssueKey issueKey)
    {
        UpdateIssueLoadProgress(wasFailure: false);
    }

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey)
    {
        UpdateIssueLoadProgress(wasFailure: true);
    }

    /// <inheritdoc />
    public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues) =>
        AnsiConsole.MarkupLine($"[green]Issue loading completed:[/] loaded = {loadedIssues.Value}, failed = {failedIssues.Value}");

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
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Issue[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Summary[/]")
            .AddColumn("[bold]Done At[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            var lastDoneAt = issue.Transitions
                .Where(x => string.Equals(x.To.Value, doneStatusName.Value, StringComparison.OrdinalIgnoreCase))
                .Select(x => (DateTimeOffset?)x.At)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var lastDoneAtText = lastDoneAt.HasValue
                ? lastDoneAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "-";

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
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
    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        MonthLabel monthLabel,
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(releases);

        AnsiConsole.MarkupLine("[bold]Release report[/]");
        AnsiConsole.MarkupLine(
            $"[grey]Project:[/] {Markup.Escape(settings.ReleaseProjectKey.Value)}    [grey]Label:[/] {Markup.Escape(settings.ProjectLabel)}    [grey]Month:[/] {Markup.Escape(monthLabel.Value)}");
        if (!string.IsNullOrWhiteSpace(settings.ComponentsFieldName))
        {
            AnsiConsole.MarkupLine($"[grey]Components field:[/] {Markup.Escape(settings.ComponentsFieldName)}");
        }

        if (releases.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No releases found for selected month.[/]");
            return;
        }

        var includeComponents = !string.IsNullOrWhiteSpace(settings.ComponentsFieldName);

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Release Date[/]")
            .AddColumn("[bold]Jira ID[/]")
            .AddColumn("[bold]Tasks[/]");

        if (includeComponents)
        {
            _ = table.AddColumn("[bold]Components[/]");
        }

        _ = table.AddColumn("[bold]Title[/]");

        var orderedReleases = releases
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedReleases.Count; i++)
        {
            var release = orderedReleases[i];
            var row = new List<string>
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Markup.Escape(release.Key.Value),
                release.Tasks.ToString(CultureInfo.InvariantCulture)
            };
            if (includeComponents)
            {
                row.Add(release.Components.ToString(CultureInfo.InvariantCulture));
            }

            row.Add(Markup.Escape(release.Title.Truncate(new TextLength(120)).Value));
            _ = table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);

        var bugTypes = bugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        AnsiConsole.MarkupLine($"[grey]Loading bug ratio data for:[/] {Markup.Escape(bugTypes)}");
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        AnsiConsole.MarkupLine(
            $"[green]Bug ratio data loaded:[/] created = {createdThisMonth.Value}, done = {movedToDoneThisMonth.Value}, rejected = {rejectedThisMonth.Value}, finished = {finishedThisMonth.Value}");
    }

    /// <inheritdoc />
    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth,
        IReadOnlyList<IssueListItem> openIssues,
        IReadOnlyList<IssueListItem> doneIssues,
        IReadOnlyList<IssueListItem> rejectedIssues)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        ArgumentNullException.ThrowIfNull(openIssues);
        ArgumentNullException.ThrowIfNull(doneIssues);
        ArgumentNullException.ThrowIfNull(rejectedIssues);

        if (bugIssueNames.Count == 0)
        {
            return;
        }

        var bugTypes = string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        var ratioText = createdThisMonth.Value == 0
            ? "n/a"
            : $"{finishedThisMonth.Value * 100.0 / createdThisMonth.Value:0.##}%";

        AnsiConsole.MarkupLine("[bold]Bug ratio[/]");

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow("Bug issue types", Markup.Escape(bugTypes));
        _ = table.AddRow("[red]Open this month[/]", $"[red]{openIssues.Count.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[green]Done this month[/]", $"[green]{movedToDoneThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[orange1]Rejected this month[/]", $"[orange1]{rejectedThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("[deepskyblue1]Finished this month[/]", $"[deepskyblue1]{finishedThisMonth.Value.ToString(CultureInfo.InvariantCulture)}[/]");
        _ = table.AddRow("Finished / Created", ratioText);

        AnsiConsole.Write(table);

        if (openIssues.Count == 0 && doneIssues.Count == 0 && rejectedIssues.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Bug ratio details[/]");
        AnsiConsole.MarkupLine("[bold red]Open issues[/]");
        RenderBugIssueDetailsTable(openIssues, "red");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Done issues[/]");
        RenderBugIssueDetailsTable(doneIssues, "green");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold orange1]Rejected issues[/]");
        RenderBugIssueDetailsTable(rejectedIssues, "orange1");
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

    private static void RenderBugIssueDetailsTable(IReadOnlyList<IssueListItem> issues, string titleColor)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No issues.[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Jira ID[/]")
            .AddColumn("[bold]Title[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issue.Key.Value),
                $"[{titleColor}]{Markup.Escape(issue.Title.Truncate(new TextLength(120)).Value)}[/]");
        }

        AnsiConsole.Write(table);
    }

    private void UpdateIssueLoadProgress(bool wasFailure)
    {
        if (_issueLoadTotal <= 0)
        {
            return;
        }

        _issueLoadProcessed++;
        if (wasFailure)
        {
            _issueLoadFailed++;
        }

        if (_issueLoadProcessed == _issueLoadTotal
            || _issueLoadProcessed % _issueLoadStep == 0)
        {
            var percent = _issueLoadProcessed * 100.0 / _issueLoadTotal;
            AnsiConsole.MarkupLine(
                $"[grey]Loading issue timelines:[/] {_issueLoadProcessed}/{_issueLoadTotal} ({percent:0}%)  [grey]failed:[/] {_issueLoadFailed}");
        }
    }

    private int _issueLoadTotal;
    private int _issueLoadProcessed;
    private int _issueLoadFailed;
    private int _issueLoadStep = 1;
}
