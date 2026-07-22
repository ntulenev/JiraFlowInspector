using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Provides formatting helpers used by PDF report sections.
/// </summary>
internal static class PresentationFormatting
{
    public const string OPEN_ISSUE_COLOR_HEX = "#dc2626";
    public const string DONE_ISSUE_COLOR_HEX = "#16a34a";
    public const string REJECTED_ISSUE_COLOR_HEX = "#f97316";

    public static string BuildLastStatusAtText(IssueTimeline issue, StatusName statusName)
    {
        var lastTimestamp = issue.TryGetLastReachedAt(statusName);

        return lastTimestamp.HasValue
            ? lastTimestamp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";
    }

    public static string BuildWorkDurationText(
        IssueTimeline issue,
        StatusName targetStatusName,
        bool showTimeCalculationsInHoursOnly)
    {
        var workDuration = issue.TryBuildWorkDuration(targetStatusName);
        if (!workDuration.HasValue)
        {
            return "-";
        }

        return FormatWorkDurationValue(workDuration.Value, showTimeCalculationsInHoursOnly);
    }

    public static string FormatIncidentDateTimeUtc(DateTimeOffset? value) =>
        value.HasValue
            ? value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";

    public static string FormatIncidentDuration(TimeSpan? duration, bool showTimeCalculationsInHoursOnly)
    {
        if (!duration.HasValue || duration.Value < TimeSpan.Zero)
        {
            return "-";
        }

        if (showTimeCalculationsInHoursOnly)
        {
            return DurationLabel.FromDuration(duration.Value, showTimeCalculationsInHoursOnly: true).Value;
        }

        var totalMinutes = (int)Math.Round(duration.Value.TotalMinutes, MidpointRounding.AwayFromZero);
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

    public static string BuildFinishedToCreatedRatioText(ItemCount createdThisMonth, ItemCount finishedThisMonth) =>
        BuildRatioText(finishedThisMonth.Value, createdThisMonth.Value);

    public static string BuildRatioText(int numerator, int denominator) =>
        denominator == 0
            ? "N/A"
            : FormatPercentage(numerator * 100.0 / denominator);

    public static string FormatPercentage(double? value) =>
        value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%" : "N/A";

    public static string ToDurationLabel(
        TimeSpan duration,
        bool showTimeCalculationsInHoursOnly = false) =>
        DurationLabel.FromDuration(duration, showTimeCalculationsInHoursOnly).Value;

    public static string BuildHotFixRulesText(IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules)
    {
        return string.Join(
            "; ",
            hotFixRules
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => $"{pair.Key} = {string.Join(", ", pair.Value)}"));
    }

    public static List<(string stage, string colorHex)> BuildStageColors(List<(string stage, TimeSpan duration)> stageDurations)
    {
        var orderedStages = new List<string>();
        foreach (var (stage, _) in stageDurations)
        {
            if (!orderedStages.Contains(stage, StringComparer.OrdinalIgnoreCase))
            {
                orderedStages.Add(stage);
            }
        }

        var colorItems = new List<(string stage, string colorHex)>(orderedStages.Count);
        for (var index = 0; index < orderedStages.Count; index++)
        {
            colorItems.Add((orderedStages[index], _timelinePaletteHex[index % _timelinePaletteHex.Length]));
        }

        return colorItems;
    }

    public static List<float> BuildStageWeights(List<(string stage, TimeSpan duration)> stageDurations)
    {
        if (stageDurations.Count == 0)
        {
            return [];
        }

        return [.. stageDurations
            .Select(static segment => (float)Math.Max(0.001, Math.Max(0.0, segment.duration.TotalSeconds)))];
    }

    private static readonly string[] _timelinePaletteHex =
    [
        "#0ea5e9",
        "#3b82f6",
        "#22d3ee",
        "#22c55e",
        "#eab308",
        "#f97316",
        "#f59e0b",
        "#9ca3af"
    ];
}
