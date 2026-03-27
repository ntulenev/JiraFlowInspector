using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal static class SpectrePresentationFormatting
{
    public static string BuildWorkDurationText(
        IssueTimeline issue,
        StatusName targetStatusName,
        bool showTimeCalculationsInHoursOnly)
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

        return FormatWorkDurationValue(workDuration, showTimeCalculationsInHoursOnly);
    }

    public static string BuildLastStatusAtText(IssueTimeline issue, StatusName statusName)
    {
        var lastTimestamp = issue.Transitions
            .Where(transition => string.Equals(transition.To.Value, statusName.Value, StringComparison.OrdinalIgnoreCase))
            .Select(static transition => (DateTimeOffset?)transition.At)
            .OrderByDescending(static timestamp => timestamp)
            .FirstOrDefault();

        return lastTimestamp.HasValue
            ? lastTimestamp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";
    }

    public static string FormatIncidentDateTimeUtc(DateTimeOffset? value) =>
        value.HasValue
            ? value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";

    public static string FormatIncidentDuration(TimeSpan? duration, bool showTimeCalculationsInHoursOnly)
    {
        if (!duration.HasValue)
        {
            return "-";
        }

        var value = duration.Value;
        if (value < TimeSpan.Zero)
        {
            return "-";
        }

        if (showTimeCalculationsInHoursOnly)
        {
            return DurationLabel.FromDuration(value, showTimeCalculationsInHoursOnly: true).Value;
        }

        var totalMinutes = (int)Math.Round(value.TotalMinutes, MidpointRounding.AwayFromZero);
        var days = totalMinutes / (24 * 60);
        var hours = totalMinutes % (24 * 60) / 60;
        var minutes = totalMinutes % 60;

        if (days > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}d {1}h {2}m", days, hours, minutes);
        }

        if (hours > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}h {1}m", hours, minutes);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}m", minutes);
    }

    public static string FormatWorkDurationValue(TimeSpan duration, bool showTimeCalculationsInHoursOnly) =>
        (showTimeCalculationsInHoursOnly ? duration.TotalHours : duration.TotalDays)
        .ToString("0.##", CultureInfo.InvariantCulture);

    public static string FormatCalendarDayDurationValue(TimeSpan duration) =>
        Math.Max(0, duration.TotalDays).ToString("0.##", CultureInfo.InvariantCulture);

    public static string GetWorkDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours at work" : "Days at work";

    public static string GetWorkDuration75Title(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours at Work 75P" : "Days at Work 75P";

    public static TimeSpan? SumIncidentDurations(IReadOnlyList<GlobalIncidentItem> incidents)
    {
        var durations = incidents
            .Select(static incident => incident.Duration)
            .Where(static duration => duration.HasValue && duration.Value >= TimeSpan.Zero)
            .Select(static duration => duration!.Value)
            .ToList();

        if (durations.Count == 0)
        {
            return null;
        }

        return durations.Aggregate(TimeSpan.Zero, static (sum, duration) => sum + duration);
    }

    public static IReadOnlyList<(string componentName, int releaseCount)> BuildComponentReleaseSummaries(
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

    public static string FormatReleaseCell(string value, bool isHotFix)
    {
        var escaped = Markup.Escape(value);
        return isHotFix ? $"[red]{escaped}[/]" : escaped;
    }

    public static string FormatExecutionDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromSeconds(1))
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        if (duration < TimeSpan.FromMinutes(1))
        {
            return $"{duration.TotalSeconds:0.##} s";
        }

        return duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    public static string FormatBytes(long bytes)
    {
        const long kilobyte = 1024;
        const long megabyte = 1024 * kilobyte;

        if (bytes >= megabyte)
        {
            return $"{bytes / (double)megabyte:0.##} MB";
        }

        if (bytes >= kilobyte)
        {
            return $"{bytes / (double)kilobyte:0.##} KB";
        }

        return $"{bytes} B";
    }
}
