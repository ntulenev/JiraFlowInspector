using System.Globalization;
using System.Text;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreTransitionSection
{
    private readonly bool _showTimeCalculationsInHoursOnly;

    public SpectreTransitionSection(bool showTimeCalculationsInHoursOnly)
    {
        _showTimeCalculationsInHoursOnly = showTimeCalculationsInHoursOnly;
    }

    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        if (issues.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Issues moved to Done in selected period[/]");

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
            .AddColumn($"[bold]{SpectrePresentationFormatting.GetWorkDurationColumnLabel(_showTimeCalculationsInHoursOnly)}[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            var lastDoneAtText = SpectrePresentationFormatting.BuildLastStatusAtText(issue, doneStatusName);
            var createdAtText = issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var daysAtWorkText = SpectrePresentationFormatting.BuildWorkDurationText(issue, doneStatusName, _showTimeCalculationsInHoursOnly);
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

    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        var title = SpectrePresentationFormatting.GetWorkDuration75Title(_showTimeCalculationsInHoursOnly);
        AnsiConsole.MarkupLine($"[bold]{title} per type (moved to {Markup.Escape(doneStatusName.Value)})[/]");
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
            .AddColumn($"[bold]{title}[/]");

        foreach (var summary in summaries
                     .OrderByDescending(static item => item.DaysAtWorkP75)
                     .ThenByDescending(static item => item.IssueCount.Value)
                     .ThenBy(static item => item.IssueType.Value, StringComparer.OrdinalIgnoreCase))
        {
            _ = table.AddRow(
                Markup.Escape(summary.IssueType.Value),
                summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture),
                SpectrePresentationFormatting.FormatWorkDurationValue(summary.DaysAtWorkP75, _showTimeCalculationsInHoursOnly));
        }

        AnsiConsole.Write(table);
    }

    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        AnsiConsole.MarkupLine("[bold]Issues moved to Rejected in selected period[/]");

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
            .AddColumn($"[bold]{SpectrePresentationFormatting.GetWorkDurationColumnLabel(_showTimeCalculationsInHoursOnly)}[/]");

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < orderedIssues.Count; i++)
        {
            var issue = orderedIssues[i];
            var lastRejectAtText = SpectrePresentationFormatting.BuildLastStatusAtText(issue, rejectStatusName);
            var createdAtText = issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var daysAtWorkText = SpectrePresentationFormatting.BuildWorkDurationText(issue, rejectStatusName, _showTimeCalculationsInHoursOnly);

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

    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        AnsiConsole.MarkupLine(
            $"[grey]Successful:[/] {summary.SuccessfulCount.Value}    [grey]Matched stage:[/] {summary.MatchedStageCount.Value}    [grey]Failed:[/] {summary.FailedCount.Value}    [grey]Path groups:[/] {summary.PathGroupCount.Value}");
        AnsiConsole.MarkupLine("[grey]Filter:[/] only tasks with code artefacts (pull request activity).");
    }

    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
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
            var totalTtm = DurationLabel.FromDuration(group.TotalP75, _showTimeCalculationsInHoursOnly).Value;
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
                    DurationLabel.FromDuration(transition.P75Duration, _showTimeCalculationsInHoursOnly).Value);
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
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
                .Append(new string('\u2588', segmentWidths[i]))
                .Append("[/]");
        }

        AnsiConsole.MarkupLine(bar.ToString());

        var legend = string.Join(
            "  ",
            stageColors.Select(static pair => $"[{pair.Value}]\u25A0[/] {Markup.Escape(pair.Key)}"));
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
