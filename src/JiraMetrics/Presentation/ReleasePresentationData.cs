using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared release data shared by all report renderers.
/// </summary>
internal sealed class ReleasePresentationData
{
    private ReleasePresentationData(
        IReadOnlyList<ReleaseIssueItem> releases,
        ItemCount totalCount,
        ItemCount hotFixCount,
        ItemCount rollbackCount,
        IReadOnlyList<ComponentReleaseSummary> components)
    {
        Releases = releases;
        TotalCount = totalCount;
        HotFixCount = hotFixCount;
        RollbackCount = rollbackCount;
        Components = components;
    }

    /// <summary>
    /// Creates consistently ordered release data and aggregate counts for report renderers.
    /// </summary>
    /// <param name="releases">Source release rows.</param>
    /// <returns>Prepared release presentation data.</returns>
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

    /// <summary>
    /// Gets release rows ordered by release date and issue key.
    /// </summary>
    public IReadOnlyList<ReleaseIssueItem> Releases { get; }

    /// <summary>
    /// Gets the total release count.
    /// </summary>
    public ItemCount TotalCount { get; }

    /// <summary>
    /// Gets the hot-fix release count.
    /// </summary>
    public ItemCount HotFixCount { get; }

    /// <summary>
    /// Gets the release count with rollback information.
    /// </summary>
    public ItemCount RollbackCount { get; }

    /// <summary>
    /// Gets release counts grouped and ordered by component.
    /// </summary>
    public IReadOnlyList<ComponentReleaseSummary> Components { get; }
}
