using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared release data shared by all report renderers.
/// </summary>
internal sealed record ReleasePresentationData(
    IReadOnlyList<ReleaseIssueItem> Releases,
    ItemCount TotalCount,
    ItemCount HotFixCount,
    ItemCount RollbackCount,
    IReadOnlyList<ComponentReleaseSummary> Components)
{
    public static ReleasePresentationData Create(IReadOnlyList<ReleaseIssueItem> releases)
    {
        ArgumentNullException.ThrowIfNull(releases);

        var orderedReleases = releases
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var componentCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var release in orderedReleases)
        {
            foreach (var componentName in release.ComponentNames)
            {
                if (string.IsNullOrWhiteSpace(componentName))
                {
                    continue;
                }

                var normalized = componentName.Trim();
                componentCounts[normalized] = componentCounts.TryGetValue(normalized, out var currentCount)
                    ? currentCount + 1
                    : 1;
            }
        }

        var components = componentCounts
            .Select(static pair => new ComponentReleaseSummary(
                pair.Key,
                new ItemCount(pair.Value)))
            .OrderByDescending(static summary => summary.ReleaseCount.Value)
            .ThenBy(static summary => summary.ComponentName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ReleasePresentationData(
            orderedReleases,
            new ItemCount(orderedReleases.Length),
            new ItemCount(orderedReleases.Count(static release => release.IsHotFix)),
            new ItemCount(orderedReleases.Count(static release => !string.IsNullOrWhiteSpace(release.RollbackType))),
            components);
    }
}

/// <summary>
/// Prepared release count for one component.
/// </summary>
internal sealed record ComponentReleaseSummary(
    string ComponentName,
    ItemCount ReleaseCount);

/// <summary>
/// Prepared global-incident data shared by all report renderers.
/// </summary>
internal sealed record GlobalIncidentsPresentationData(
    IReadOnlyList<GlobalIncidentItem> Incidents,
    TimeSpan? TotalDuration)
{
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
}
