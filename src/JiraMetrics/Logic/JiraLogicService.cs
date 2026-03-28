using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Default implementation of Jira application logic.
/// </summary>
public sealed class JiraLogicService : IJiraLogicService
{

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraLogicService"/> class.
    /// </summary>
    /// <param name="analytics">Analytics service.</param>
    public JiraLogicService(IJiraAnalyticsService analytics)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
    }

    /// <summary>
    /// Filters issues by required path stage.
    /// </summary>
    /// <param name="issues">Issues to filter.</param>
    /// <param name="requiredPathStages">Required stages.</param>
    /// <returns>Filtered issues.</returns>
    public IReadOnlyList<IssueTimeline> FilterIssuesByRequiredStage(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<StageName> requiredPathStages)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(requiredPathStages);

        if (requiredPathStages.Count == 0)
        {
            return issues;
        }

        return [.. new IssueTimelineSet(issues).FilterByRequiredStages(requiredPathStages)];
    }

    /// <summary>
    /// Filters issues by allowed issue types.
    /// </summary>
    /// <param name="issues">Issues to filter.</param>
    /// <param name="issueTypes">Allowed issue types.</param>
    /// <returns>Filtered issues.</returns>
    public IReadOnlyList<IssueTimeline> FilterIssuesByIssueTypes(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<IssueTypeName> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(issueTypes);

        if (issueTypes.Count == 0)
        {
            return issues;
        }

        return [.. new IssueTimelineSet(issues).FilterByIssueTypes(issueTypes)];
    }

    /// <summary>
    /// Calculates days-at-work P75 per issue type for issues that reached target status.
    /// </summary>
    /// <param name="issues">Issues to analyze.</param>
    /// <param name="targetStatusName">Target status used as work completion point.</param>
    /// <returns>P75 summaries per issue type.</returns>
    public IReadOnlyList<IssueTypeWorkDays75Summary> BuildDaysAtWork75PerType(
        IReadOnlyList<IssueTimeline> issues,
        StatusName targetStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return new IssueTimelineSet(issues).BuildDaysAtWork75PerType(
            targetStatusName,
            _analytics.CalculatePercentile);
    }

    /// <summary>
    /// Builds grouped path statistics for issues.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <returns>Path groups.</returns>
    public IReadOnlyList<PathGroup> BuildPathGroups(IReadOnlyList<IssueTimeline> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return new IssueTimelineSet(issues).BuildPathGroups(_analytics.CalculatePercentile);
    }
    private readonly IJiraAnalyticsService _analytics;

}

