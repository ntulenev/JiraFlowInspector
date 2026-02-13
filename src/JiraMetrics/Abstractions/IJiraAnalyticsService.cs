using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Provides domain analytics operations for Jira timelines.
/// </summary>
public interface IJiraAnalyticsService
{
    /// <summary>
    /// Builds a machine-readable path key from transitions.
    /// </summary>
    /// <param name="transitions">Transitions.</param>
    /// <returns>Path key.</returns>
    PathKey BuildPathKey(IReadOnlyList<TransitionEvent> transitions);

    /// <summary>
    /// Builds a human-readable path label from transitions.
    /// </summary>
    /// <param name="transitions">Transitions.</param>
    /// <returns>Path label.</returns>
    PathLabel BuildPathLabel(IReadOnlyList<TransitionEvent> transitions);

    /// <summary>
    /// Determines whether transitions contain a required stage.
    /// </summary>
    /// <param name="transitions">Transitions.</param>
    /// <param name="requiredStage">Required stage.</param>
    /// <returns><see langword="true"/> when stage is present; otherwise <see langword="false"/>.</returns>
    bool PathContainsStage(IReadOnlyList<TransitionEvent> transitions, StageName requiredStage);

    /// <summary>
    /// Calculates percentile for a duration sample set.
    /// </summary>
    /// <param name="values">Duration samples.</param>
    /// <param name="percentile">Percentile value.</param>
    /// <returns>Percentile duration.</returns>
    TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> values, PercentileValue percentile);

    /// <summary>
    /// Truncates summary to the specified length.
    /// </summary>
    /// <param name="summary">Issue summary.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Truncated summary.</returns>
    IssueSummary Truncate(IssueSummary summary, TextLength maxLength);

    /// <summary>
    /// Formats duration for presentation.
    /// </summary>
    /// <param name="duration">Duration value.</param>
    /// <returns>Duration label.</returns>
    DurationLabel FormatDuration(TimeSpan duration);
}
