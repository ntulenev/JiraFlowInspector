using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Logic;

/// <summary>
/// Provides domain analytics operations for Jira timelines.
/// </summary>
public interface IJiraAnalyticsService
{
    /// <summary>
    /// Calculates percentile for a duration sample set.
    /// </summary>
    /// <param name="values">Duration samples.</param>
    /// <param name="percentile">Percentile value.</param>
    /// <returns>Percentile duration.</returns>
    TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> values, PercentileValue percentile);
}

