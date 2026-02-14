using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Provides application domain logic for Jira analytics.
/// </summary>
public interface IJiraLogicService
{
    /// <summary>
    /// Filters issues by required stages in transition path.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <param name="requiredPathStages">Required stages.</param>
    /// <returns>Filtered issues.</returns>
    IReadOnlyList<IssueTimeline> FilterIssuesByRequiredStage(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<StageName> requiredPathStages);

    /// <summary>
    /// Filters issues by configured issue types.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <param name="issueTypes">Allowed issue types.</param>
    /// <returns>Filtered issues.</returns>
    IReadOnlyList<IssueTimeline> FilterIssuesByIssueTypes(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Groups issues by transition path and calculates per-transition P75 durations.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <returns>Path groups.</returns>
    IReadOnlyList<PathGroup> BuildPathGroups(IReadOnlyList<IssueTimeline> issues);
}
