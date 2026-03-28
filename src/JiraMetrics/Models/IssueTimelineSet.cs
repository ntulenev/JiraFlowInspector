using System.Collections;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a domain collection of issue timelines with analytics operations.
/// </summary>
public sealed class IssueTimelineSet : IReadOnlyList<IssueTimeline>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTimelineSet"/> class.
    /// </summary>
    /// <param name="issues">Issue timelines.</param>
    public IssueTimelineSet(IEnumerable<IssueTimeline> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        _issues = [.. issues];
    }

    /// <summary>
    /// Gets the number of issues in the set.
    /// </summary>
    public int Count => _issues.Count;

    /// <summary>
    /// Gets the issue at the specified index.
    /// </summary>
    /// <param name="index">Issue index.</param>
    /// <returns>Issue timeline.</returns>
    public IssueTimeline this[int index] => _issues[index];

    /// <summary>
    /// Filters issues by required transition stages.
    /// </summary>
    /// <param name="requiredStages">Required stages.</param>
    /// <returns>Filtered issue set.</returns>
    public IssueTimelineSet FilterByRequiredStages(IReadOnlyList<StageName> requiredStages)
    {
        ArgumentNullException.ThrowIfNull(requiredStages);

        return requiredStages.Count == 0
            ? this
            : new IssueTimelineSet(_issues.Where(issue => issue.MatchesAllStages(requiredStages)));
    }

    /// <summary>
    /// Filters issues by allowed issue types.
    /// </summary>
    /// <param name="issueTypes">Allowed issue types.</param>
    /// <returns>Filtered issue set.</returns>
    public IssueTimelineSet FilterByIssueTypes(IReadOnlyList<IssueTypeName> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        return issueTypes.Count == 0
            ? this
            : new IssueTimelineSet(_issues.Where(issue => issue.MatchesAnyType(issueTypes)));
    }

    /// <summary>
    /// Filters issues to items that have pull-request activity.
    /// </summary>
    /// <returns>Filtered issue set.</returns>
    public IssueTimelineSet WithPullRequests() =>
        new(_issues.Where(static issue => issue.HasPullRequest));

    /// <summary>
    /// Builds P75 work-duration summaries grouped by issue type.
    /// </summary>
    /// <param name="targetStatusName">Target status used as work completion point.</param>
    /// <param name="calculatePercentile">Percentile calculator.</param>
    /// <returns>P75 summaries per issue type.</returns>
    public IReadOnlyList<IssueTypeWorkDays75Summary> BuildDaysAtWork75PerType(
        StatusName targetStatusName,
        Func<IReadOnlyList<TimeSpan>, PercentileValue, TimeSpan> calculatePercentile)
    {
        ArgumentNullException.ThrowIfNull(calculatePercentile);

        return [.. _issues
            .Select(issue => (issue.IssueType, workDuration: issue.TryBuildWorkDuration(targetStatusName)))
            .Where(static sample => sample.workDuration.HasValue)
            .GroupBy(static sample => sample.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var issueType = group.First().IssueType;
                var samples = group
                    .Select(static sample => sample.workDuration!.Value)
                    .ToList();
                var p75 = calculatePercentile(samples, new PercentileValue(0.75));

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
    /// Groups issues by transition path and calculates per-transition P75 durations.
    /// </summary>
    /// <param name="calculatePercentile">Percentile calculator.</param>
    /// <returns>Path groups.</returns>
    public IReadOnlyList<PathGroup> BuildPathGroups(
        Func<IReadOnlyList<TimeSpan>, PercentileValue, TimeSpan> calculatePercentile)
    {
        ArgumentNullException.ThrowIfNull(calculatePercentile);

        return [.. _issues
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

                    var p75 = calculatePercentile(samples, new PercentileValue(0.75));
                    p75Transitions.Add(new PercentileTransition(template[i].From, template[i].To, p75));
                }

                return PathGroup.Create(groupedIssues[0].PathLabel, groupedIssues, p75Transitions);
            })
            .OrderByDescending(static group => group.Issues.Count)
            .ThenBy(static group => group.PathLabel.Value, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Returns an enumerator over issues in the set.
    /// </summary>
    /// <returns>Enumerator.</returns>
    public IEnumerator<IssueTimeline> GetEnumerator() => _issues.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly IReadOnlyList<IssueTimeline> _issues;
}
