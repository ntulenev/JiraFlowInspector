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
    public void ShowReportPeriodContext(MonthLabel monthLabel, CreatedAfterDate? createdAfter)
    {
        AnsiConsole.MarkupLine($"[grey]Month label:[/] {Markup.Escape(monthLabel.Value)}");
        if (createdAfter is { } value)
        {
            AnsiConsole.MarkupLine($"[grey]Created after:[/] {Markup.Escape(value.ToString())}");
        }
    }

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
                $"[grey]Filtered by:[/] {Markup.Escape(settings.CustomFieldName)} = {Markup.Escape(settings.CustomFieldValue)}");
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
    public void ShowIssueLoaded(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: false);

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: true);

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
            .AddColumn("[bold]Sub-items[/]")
            .AddColumn("[bold]Code[/]")
            .AddColumn("[bold]Summary[/]")
            .AddColumn("[bold]Created At[/]")
            .AddColumn("[bold]Done At[/]")
            .AddColumn("[bold]Days at work[/]");

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
            var createdAtText = issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var daysAtWorkText = BuildWorkDaysAtText(issue, doneStatusName);
            var codeText = issue.HasPullRequest ? "[green]+[/]" : string.Empty;

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issue.Key.Value),
                Markup.Escape(issue.IssueType.Value),
                issue.SubItemsCount.ToString(CultureInfo.InvariantCulture),
                codeText,
                Markup.Escape(issue.Summary.Truncate(new TextLength(120)).Value),
                Markup.Escape(createdAtText),
                Markup.Escape(lastDoneAtText),
                Markup.Escape(daysAtWorkText));
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        AnsiConsole.MarkupLine($"[bold]Days at Work 75P per type (moved to {Markup.Escape(doneStatusName.Value)})[/]");
        if (summaries.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No data.[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Issues[/]")
            .AddColumn("[bold]Days at Work 75P[/]");

        foreach (var summary in summaries
                     .OrderByDescending(static item => item.DaysAtWorkP75)
                     .ThenByDescending(static item => item.IssueCount.Value)
                     .ThenBy(static item => item.IssueType.Value, StringComparer.OrdinalIgnoreCase))
        {
            _ = table.AddRow(
                Markup.Escape(summary.IssueType.Value),
                summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture),
                summary.DaysAtWorkP75.TotalDays.ToString("0.##", CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);

        AnsiConsole.MarkupLine("[bold]Issues moved to Rejected this month[/]");

        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No issues[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Issue[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Summary[/]")
            .AddColumn("[bold]Created At[/]")
            .AddColumn("[bold]Rejected At[/]")
            .AddColumn("[bold]Days at work[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            var lastRejectAt = issue.Transitions
                .Where(x => string.Equals(x.To.Value, rejectStatusName.Value, StringComparison.OrdinalIgnoreCase))
                .Select(x => (DateTimeOffset?)x.At)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            var lastRejectAtText = lastRejectAt.HasValue
                ? lastRejectAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : "-";
            var createdAtText = issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var daysAtWorkText = BuildWorkDaysAtText(issue, rejectStatusName);

            _ = table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issue.Key.Value),
                Markup.Escape(issue.IssueType.Value),
                Markup.Escape(issue.Summary.Truncate(new TextLength(120)).Value),
                Markup.Escape(createdAtText),
                Markup.Escape(lastRejectAtText),
                Markup.Escape(daysAtWorkText));
        }

        AnsiConsole.Write(table);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        AnsiConsole.MarkupLine(
            $"[grey]Successful:[/] {summary.SuccessfulCount.Value}    [grey]Matched stage:[/] {summary.MatchedStageCount.Value}    [grey]Failed:[/] {summary.FailedCount.Value}    [grey]Path groups:[/] {summary.PathGroupCount.Value}");
        AnsiConsole.MarkupLine("[grey]Filter:[/] only tasks with code artefacts (pull request activity).");
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
        AnsiConsole.MarkupLine($"[bold red]All releases by label \"{Markup.Escape(settings.ProjectLabel)}\"[/]");
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
            .AddColumn("[bold]Status[/]")
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
                Markup.Escape(release.Status.Value),
                release.Tasks == 0
                    ? "-"
                    : release.Tasks.ToString(CultureInfo.InvariantCulture)
            };
            if (includeComponents)
            {
                row.Add(release.Components == 0
                    ? "-"
                    : release.Components.ToString(CultureInfo.InvariantCulture));
            }

            row.Add(Markup.Escape(release.Title.Truncate(new TextLength(120)).Value));
            _ = table.AddRow([.. row]);
        }

        AnsiConsole.Write(table);

        if (!includeComponents)
        {
            return;
        }

        var componentSummaries = BuildComponentReleaseSummaries(orderedReleases);
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
        string? customFieldName,
        string? customFieldValue,
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
        if (!string.IsNullOrWhiteSpace(customFieldName)
            && !string.IsNullOrWhiteSpace(customFieldValue))
        {
            AnsiConsole.MarkupLine(
                $"[grey]Filtered by:[/] {Markup.Escape(customFieldName)} = {Markup.Escape(customFieldValue)}");
        }

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
        RenderBugIssueDetailsTable(openIssues, "red", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Done issues[/]");
        RenderBugIssueDetailsTable(doneIssues, "green", includeCreationDate: true);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold orange1]Rejected issues[/]");
        RenderBugIssueDetailsTable(rejectedIssues, "orange1", includeCreationDate: false);
    }

    /// <inheritdoc />
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(statusSummaries);

        var excludedStatuses = rejectStatusName is { } rejectStatus
            ? $"{doneStatusName.Value}, {rejectStatus.Value}"
            : doneStatusName.Value;
        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

        AnsiConsole.MarkupLine("[bold]General statistics[/]");
        AnsiConsole.MarkupLine($"[grey]Data as of:[/] {Markup.Escape(generatedAt)}");
        AnsiConsole.MarkupLine("[grey]Scope:[/] all not finished tasks");
        AnsiConsole.MarkupLine($"[grey]Statuses excluded:[/] {Markup.Escape(excludedStatuses)}");

        if (statusSummaries.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No issues outside excluded statuses.[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Issues[/]")
            .AddColumn("[bold]Breakdown by type[/]");

        foreach (var statusSummary in statusSummaries
                     .OrderByDescending(static summary => summary.Count.Value)
                     .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase))
        {
            var issueTypeBreakdown = statusSummary.IssueTypes.Count == 0
                ? "-"
                : string.Join(
                    Environment.NewLine,
                    statusSummary.IssueTypes
                        .OrderByDescending(static summary => summary.Count.Value)
                        .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                        .Select(summary =>
                            $"{summary.IssueType.Value} - {summary.Count.Value.ToString(CultureInfo.InvariantCulture)}"));

            _ = table.AddRow(
                Markup.Escape(statusSummary.Status.Value),
                statusSummary.Count.Value.ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issueTypeBreakdown));
        }

        AnsiConsole.Write(table);
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

    private static void RenderBugIssueDetailsTable(
        IReadOnlyList<IssueListItem> issues,
        string titleColor,
        bool includeCreationDate)
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

    private static string BuildWorkDaysAtText(IssueTimeline issue, StatusName targetStatusName)
    {
        var targetTransitionIndex = issue.Transitions
            .Select(static (transition, index) => (transition, index))
            .Where(item => string.Equals(item.transition.To.Value, targetStatusName.Value, StringComparison.OrdinalIgnoreCase))
            .Select(static item => item.index)
            .DefaultIfEmpty(-1)
            .Max();
        if (targetTransitionIndex < 0)
        {
            return "-";
        }

        var workDuration = issue.Transitions
            .Take(targetTransitionIndex + 1)
            .Aggregate(TimeSpan.Zero, static (sum, transition) => sum + transition.SincePrevious);

        return workDuration.TotalDays.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<(string componentName, int releaseCount)> BuildComponentReleaseSummaries(
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var release in releases)
        {
            foreach (var componentName in release.ComponentNames)
            {
                if (string.IsNullOrWhiteSpace(componentName))
                {
                    continue;
                }

                var normalized = componentName.Trim();
                counts[normalized] = counts.TryGetValue(normalized, out var currentCount)
                    ? currentCount + 1
                    : 1;
            }
        }

        return [.. counts
            .Select(static pair => (componentName: pair.Key, releaseCount: pair.Value))
            .OrderByDescending(static pair => pair.releaseCount)
            .ThenBy(static pair => pair.componentName, StringComparer.OrdinalIgnoreCase)];
    }
}
