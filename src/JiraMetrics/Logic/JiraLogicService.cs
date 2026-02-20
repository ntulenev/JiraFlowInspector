using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Default implementation of Jira application logic.
/// </summary>
public sealed class JiraLogicService : IJiraLogicService
{
    private readonly IJiraAnalyticsService _analytics;

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

        return [.. issues.Where(issue =>
            requiredPathStages.All(stage => issue.Transitions.Any(stage.IsUsedInTransition)))];
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

        var allowedTypes = issueTypes
            .Select(static issueType => issueType.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. issues.Where(issue => allowedTypes.Contains(issue.IssueType.Value))];
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

        return [.. issues
            .Select(issue => (issue.IssueType, workDuration: TryBuildWorkDuration(issue, targetStatusName)))
            .Where(static sample => sample.workDuration.HasValue)
            .GroupBy(static sample => sample.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var issueType = group.First().IssueType;
                var samples = group
                    .Select(static sample => sample.workDuration!.Value)
                    .ToList();
                var p75 = _analytics.CalculatePercentile(samples, new PercentileValue(0.75));

                return new IssueTypeWorkDays75Summary(
                    issueType,
                    new ItemCount(samples.Count),
                    p75);
            })
            .OrderByDescending(static summary => summary.DaysAtWorkP75)
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

        return [.. issues
            .GroupBy(issue => issue.PathKey.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var groupedIssues = group.ToList();
                var template = groupedIssues[0].Transitions;
                var p75Transitions = new List<PercentileTransition>(template.Count);

                for (var i = 0; i < template.Count; i++)
                {
                    var samples = groupedIssues
                        .Select(issue => issue.Transitions[i].SincePrevious)
                        .ToList();

                    var p75 = _analytics.CalculatePercentile(samples, new PercentileValue(0.75));
                    p75Transitions.Add(new PercentileTransition(template[i].From, template[i].To, p75));
                }

                var totalP75 = p75Transitions.Aggregate(TimeSpan.Zero, (acc, item) => acc + item.P75Duration);
                return new PathGroup(groupedIssues[0].PathLabel, groupedIssues, p75Transitions, totalP75);
            })
            .OrderByDescending(group => group.Issues.Count)
            .ThenBy(group => group.PathLabel.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static TimeSpan? TryBuildWorkDuration(IssueTimeline issue, StatusName targetStatusName)
    {
        var targetTransitionIndex = issue.Transitions
            .Select(static (transition, index) => (transition, index))
            .Where(item => string.Equals(item.transition.To.Value, targetStatusName.Value, StringComparison.OrdinalIgnoreCase))
            .Select(static item => item.index)
            .DefaultIfEmpty(-1)
            .Max();
        if (targetTransitionIndex < 0)
        {
            return null;
        }

        var duration = issue.Transitions
            .Take(targetTransitionIndex + 1)
            .Aggregate(TimeSpan.Zero, static (sum, transition) => sum + transition.SincePrevious);

        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }
}
