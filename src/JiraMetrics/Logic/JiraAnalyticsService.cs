using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Default implementation of Jira analytics operations.
/// </summary>
public sealed class JiraAnalyticsService : IJiraAnalyticsService
{
    /// <summary>
    /// Builds path key from transitions.
    /// </summary>
    /// <param name="transitions">Transition events.</param>
    /// <returns>Path key.</returns>
    public PathKey BuildPathKey(IReadOnlyList<TransitionEvent> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        if (transitions.Count == 0)
        {
            return new PathKey("__NO_TRANSITIONS__");
        }

        return new PathKey(string.Join(
            "||",
            transitions.Select(x => $"{x.From.Value.ToUpperInvariant()}->{x.To.Value.ToUpperInvariant()}")));
    }

    /// <summary>
    /// Builds path label from transitions.
    /// </summary>
    /// <param name="transitions">Transition events.</param>
    /// <returns>Path label.</returns>
    public PathLabel BuildPathLabel(IReadOnlyList<TransitionEvent> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        if (transitions.Count == 0)
        {
            return new PathLabel("No transitions");
        }

        return new PathLabel(string.Join(" | ", transitions.Select(x => $"{x.From} -> {x.To}")));
    }

    /// <summary>
    /// Checks whether transitions contain required stage.
    /// </summary>
    /// <param name="transitions">Transition events.</param>
    /// <param name="requiredStage">Required stage.</param>
    /// <returns><see langword="true"/> if stage is present; otherwise <see langword="false"/>.</returns>
    public bool PathContainsStage(IReadOnlyList<TransitionEvent> transitions, StageName requiredStage)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        return transitions.Any(transition =>
            transition.From.Value.Contains(requiredStage.Value, StringComparison.OrdinalIgnoreCase) ||
            transition.To.Value.Contains(requiredStage.Value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculates percentile value for a duration collection.
    /// </summary>
    /// <param name="values">Duration samples.</param>
    /// <param name="percentile">Percentile value.</param>
    /// <returns>Percentile duration.</returns>
    public TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> values, PercentileValue percentile)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var sorted = values
            .Select(value => Math.Max(0.0, value.TotalSeconds))
            .OrderBy(value => value)
            .ToList();

        if (sorted.Count == 1)
        {
            return TimeSpan.FromSeconds(sorted[0]);
        }

        var rank = (sorted.Count - 1) * percentile.Value;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        var fraction = rank - lowerIndex;
        var interpolated = sorted[lowerIndex] + ((sorted[upperIndex] - sorted[lowerIndex]) * fraction);

        return TimeSpan.FromSeconds(interpolated);
    }

    /// <summary>
    /// Truncates summary to a specific maximum length.
    /// </summary>
    /// <param name="summary">Issue summary.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Truncated summary.</returns>
    public IssueSummary Truncate(IssueSummary summary, TextLength maxLength)
    {
        if (summary.Value.Length <= maxLength.Value)
        {
            return summary;
        }

        return new IssueSummary(summary.Value[..(maxLength.Value - 3)] + "...");
    }

    /// <summary>
    /// Formats duration for console output.
    /// </summary>
    /// <param name="duration">Duration.</param>
    /// <returns>Duration label.</returns>
    public DurationLabel FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        var days = (int)duration.TotalDays;
        var hours = duration.Hours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;

        var parts = new List<string>();
        if (days > 0)
        {
            parts.Add($"{days}d");
        }

        if (hours > 0)
        {
            parts.Add($"{hours}h");
        }

        if (minutes > 0)
        {
            parts.Add($"{minutes}m");
        }

        if (parts.Count == 0)
        {
            parts.Add($"{seconds}s");
        }

        return new DurationLabel(string.Join(" ", parts));
    }
}
