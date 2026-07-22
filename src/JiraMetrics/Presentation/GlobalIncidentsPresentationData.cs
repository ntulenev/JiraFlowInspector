using JiraMetrics.Models;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared global-incident data shared by all report renderers.
/// </summary>
internal sealed class GlobalIncidentsPresentationData
{
    private GlobalIncidentsPresentationData(
        IReadOnlyList<GlobalIncidentItem> incidents,
        TimeSpan? totalDuration)
    {
        Incidents = incidents;
        TotalDuration = totalDuration;
    }

    /// <summary>
    /// Creates consistently ordered incident data and its aggregate duration.
    /// </summary>
    /// <param name="incidents">Source global-incident rows.</param>
    /// <returns>Prepared global-incident presentation data.</returns>
    public static GlobalIncidentsPresentationData Create(IReadOnlyList<GlobalIncidentItem> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        var orderedIncidents = incidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var durations = orderedIncidents
            .Select(static incident => incident.Duration)
            .Where(static duration => duration.HasValue && duration.Value >= TimeSpan.Zero)
            .Select(static duration => duration!.Value)
            .ToArray();

        return new GlobalIncidentsPresentationData(
            orderedIncidents,
            durations.Length == 0
                ? null
                : durations.Aggregate(TimeSpan.Zero, static (sum, duration) => sum + duration));
    }

    /// <summary>
    /// Gets incident rows ordered by start timestamp and issue key.
    /// </summary>
    public IReadOnlyList<GlobalIncidentItem> Incidents { get; }

    /// <summary>
    /// Gets the sum of all valid incident durations, or <see langword="null"/> when none are available.
    /// </summary>
    public TimeSpan? TotalDuration { get; }
}
