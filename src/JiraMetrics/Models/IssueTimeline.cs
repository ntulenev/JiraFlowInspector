using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a Jira issue with its transition timeline.
/// </summary>
public sealed record IssueTimeline
{
    /// <summary>
    /// Creates a normalized issue timeline and derives path metadata from transitions.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="issueType">Issue type.</param>
    /// <param name="summary">Issue summary.</param>
    /// <param name="created">Issue creation timestamp.</param>
    /// <param name="transitions">Ordered status transition events.</param>
    /// <param name="endTime">Optional issue end timestamp used for analytics.</param>
    /// <param name="subItemsCount">Number of sub-items.</param>
    /// <param name="hasPullRequest">Whether issue has linked pull request(s).</param>
    /// <returns>Normalized issue timeline.</returns>
    public static IssueTimeline Create(
        IssueKey key,
        IssueTypeName issueType,
        IssueSummary summary,
        DateTimeOffset created,
        IReadOnlyList<TransitionEvent> transitions,
        DateTimeOffset? endTime = null,
        int subItemsCount = 0,
        bool hasPullRequest = false)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        var resolvedEndTime = endTime ?? DateTimeOffset.UtcNow;
        if (resolvedEndTime < created)
        {
            resolvedEndTime = created;
        }

        return new IssueTimeline(
            key,
            issueType,
            summary,
            created,
            resolvedEndTime,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            subItemsCount,
            hasPullRequest);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTimeline"/> class.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="issueType">Issue type.</param>
    /// <param name="summary">Issue summary.</param>
    /// <param name="created">Issue creation timestamp.</param>
    /// <param name="endTime">Issue end timestamp used for analytics.</param>
    /// <param name="transitions">Status transition events.</param>
    /// <param name="pathKey">Machine-readable path key.</param>
    /// <param name="pathLabel">Human-readable path label.</param>
    /// <param name="subItemsCount">Number of sub-items.</param>
    /// <param name="hasPullRequest">Whether issue has linked pull request(s).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="endTime"/> is earlier than <paramref name="created"/>.</exception>
    public IssueTimeline(
        IssueKey key,
        IssueTypeName issueType,
        IssueSummary summary,
        DateTimeOffset created,
        DateTimeOffset endTime,
        IReadOnlyList<TransitionEvent> transitions,
        PathKey pathKey,
        PathLabel pathLabel,
        int subItemsCount = 0,
        bool hasPullRequest = false)
    {
        Key = key;
        IssueType = issueType;
        Summary = summary;
        Created = created;
        EndTime = endTime;
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
        PathKey = pathKey;
        PathLabel = pathLabel;
        SubItemsCount = subItemsCount;
        HasPullRequest = hasPullRequest;

        if (EndTime < Created)
        {
            throw new ArgumentException("End time cannot be earlier than created time.", nameof(endTime));
        }

        if (SubItemsCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subItemsCount), "Sub-items count cannot be negative.");
        }
    }

    /// <summary>
    /// Gets the issue key.
    /// </summary>
    public IssueKey Key { get; }

    /// <summary>
    /// Gets the issue summary.
    /// </summary>
    public IssueSummary Summary { get; }

    /// <summary>
    /// Gets the issue type.
    /// </summary>
    public IssueTypeName IssueType { get; }

    /// <summary>
    /// Gets the issue creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; }

    /// <summary>
    /// Gets the issue end timestamp used for analytics.
    /// </summary>
    public DateTimeOffset EndTime { get; }

    /// <summary>
    /// Gets ordered status transitions.
    /// </summary>
    public IReadOnlyList<TransitionEvent> Transitions { get; }

    /// <summary>
    /// Gets machine-readable path key.
    /// </summary>
    public PathKey PathKey { get; }

    /// <summary>
    /// Gets human-readable path label.
    /// </summary>
    public PathLabel PathLabel { get; }

    /// <summary>
    /// Gets number of sub-items.
    /// </summary>
    public int SubItemsCount { get; }

    /// <summary>
    /// Gets a value indicating whether the issue has pull request(s).
    /// </summary>
    public bool HasPullRequest { get; }

    /// <summary>
    /// Determines whether the issue includes the specified stage in any transition.
    /// </summary>
    /// <param name="stageName">Stage name.</param>
    /// <returns><see langword="true"/> when the stage appears in the transition path.</returns>
    public bool UsesStage(StageName stageName) => Transitions.Any(stageName.IsUsedInTransition);

    /// <summary>
    /// Determines whether the issue contains all required stages.
    /// </summary>
    /// <param name="requiredStages">Required stages.</param>
    /// <returns><see langword="true"/> when every required stage is present.</returns>
    public bool MatchesAllStages(IReadOnlyList<StageName> requiredStages)
    {
        ArgumentNullException.ThrowIfNull(requiredStages);

        return requiredStages.Count == 0 || requiredStages.All(UsesStage);
    }

    /// <summary>
    /// Determines whether the issue type matches the specified issue type.
    /// </summary>
    /// <param name="issueType">Issue type.</param>
    /// <returns><see langword="true"/> when issue types match ignoring case.</returns>
    public bool IsOfType(IssueTypeName issueType) =>
        string.Equals(IssueType.Value, issueType.Value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the issue matches any of the provided issue types.
    /// </summary>
    /// <param name="issueTypes">Allowed issue types.</param>
    /// <returns><see langword="true"/> when the issue matches at least one type.</returns>
    public bool MatchesAnyType(IReadOnlyList<IssueTypeName> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        return issueTypes.Count == 0 || issueTypes.Any(IsOfType);
    }

    /// <summary>
    /// Tries to get the timestamp of the last transition into the target status.
    /// </summary>
    /// <param name="targetStatusName">Target status.</param>
    /// <returns>Timestamp of the last transition into the target status, or <c>null</c>.</returns>
    public DateTimeOffset? TryGetLastReachedAt(StatusName targetStatusName)
    {
        var targetTransitionIndex = GetLastReachedTransitionIndex(targetStatusName);
        return targetTransitionIndex < 0
            ? null
            : Transitions[targetTransitionIndex].At;
    }

    /// <summary>
    /// Tries to calculate cumulative work duration until the issue first reaches the target status.
    /// </summary>
    /// <param name="targetStatusName">Target status used as work completion point.</param>
    /// <returns>
    /// Total duration up to the last transition into the target status, or <c>null</c> when the target status
    /// was never reached.
    /// </returns>
    public TimeSpan? TryBuildWorkDuration(StatusName targetStatusName)
    {
        var targetTransitionIndex = GetLastReachedTransitionIndex(targetStatusName);
        if (targetTransitionIndex < 0)
        {
            return null;
        }

        var duration = Transitions
            .Take(targetTransitionIndex + 1)
            .Aggregate(TimeSpan.Zero, static (sum, transition) => sum + transition.SincePrevious);

        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    private int GetLastReachedTransitionIndex(StatusName targetStatusName)
    {
        return Transitions
            .Select(static (transition, index) => (transition, index))
            .Where(item => string.Equals(
                item.transition.To.Value,
                targetStatusName.Value,
                StringComparison.OrdinalIgnoreCase))
            .Select(static item => item.index)
            .DefaultIfEmpty(-1)
            .Max();
    }
}
