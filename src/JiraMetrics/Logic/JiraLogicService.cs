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
    /// <param name="requiredPathStage">Required stage.</param>
    /// <returns>Filtered issues.</returns>
    public IReadOnlyList<IssueTimeline> FilterIssuesByRequiredStage(
        IReadOnlyList<IssueTimeline> issues,
        StageName requiredPathStage)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return [.. issues.Where(issue => issue.Transitions.Any(requiredPathStage.IsUsedInTransition))];
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
}
