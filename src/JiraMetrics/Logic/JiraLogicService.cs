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
    /// Builds issues that contain a specific status transition.
    /// </summary>
    /// <param name="doneIssues">Done issues.</param>
    /// <param name="rejectedIssues">Rejected issues.</param>
    /// <param name="fromStatusName">Source status.</param>
    /// <param name="toStatusName">Destination status.</param>
    /// <param name="codeOnly">Whether only issues with code activity should be included.</param>
    /// <returns>Matching issues ordered by transition duration descending.</returns>
    public IReadOnlyList<CustomTransitionIssue> BuildCustomTransitionIssues(
        IReadOnlyList<IssueTimeline> doneIssues,
        IReadOnlyList<IssueTimeline> rejectedIssues,
        StatusName fromStatusName,
        StatusName toStatusName,
        bool codeOnly)
    {
        ArgumentNullException.ThrowIfNull(doneIssues);
        ArgumentNullException.ThrowIfNull(rejectedIssues);

        return [.. doneIssues
            .Concat(rejectedIssues)
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Where(issue => !codeOnly || issue.HasPullRequest)
            .Select(issue => (issue, transition: issue.TryGetLastTransition(fromStatusName, toStatusName)))
            .Where(static item => item.transition is not null)
            .Select(static item => new CustomTransitionIssue(
                item.issue,
                item.transition!.At,
                item.transition.SincePrevious))
            .OrderByDescending(static item => item.Duration)
            .ThenBy(static item => item.Issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Calculates P75 transition duration grouped by issue type.
    /// </summary>
    /// <param name="issues">Custom transition issues.</param>
    /// <returns>P75 summaries per issue type.</returns>
    public IReadOnlyList<IssueTypeDuration75Summary> BuildDuration75PerType(
        IReadOnlyList<CustomTransitionIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return [.. issues
            .GroupBy(static issue => issue.Issue.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var issueType = group.First().Issue.IssueType;
                var samples = group
                    .Select(static issue => issue.Duration)
                    .ToList();
                var p75 = _analytics.CalculatePercentile(samples, new PercentileValue(0.75));

                return new IssueTypeDuration75Summary(
                    issueType,
                    new ItemCount(samples.Count),
                    p75);
            })
            .OrderByDescending(static summary => summary.DurationP75)
            .ThenByDescending(static summary => summary.IssueCount.Value)
            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)];
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

