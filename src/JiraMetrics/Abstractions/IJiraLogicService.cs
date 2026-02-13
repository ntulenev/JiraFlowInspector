using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Provides application domain logic for Jira analytics.
/// </summary>
public interface IJiraLogicService
{
    /// <summary>
    /// Filters issues by required stage in transition path.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <param name="requiredPathStage">Required stage.</param>
    /// <returns>Filtered issues.</returns>
    IReadOnlyList<IssueTimeline> FilterIssuesByRequiredStage(
        IReadOnlyList<IssueTimeline> issues,
        StageName requiredPathStage);

    /// <summary>
    /// Groups issues by transition path and calculates per-transition P75 durations.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <returns>Path groups.</returns>
    IReadOnlyList<PathGroup> BuildPathGroups(IReadOnlyList<IssueTimeline> issues);
}
