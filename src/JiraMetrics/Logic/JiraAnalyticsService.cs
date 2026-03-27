using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Default implementation of Jira analytics operations.
/// </summary>
public sealed class JiraAnalyticsService : IJiraAnalyticsService
{
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

}

