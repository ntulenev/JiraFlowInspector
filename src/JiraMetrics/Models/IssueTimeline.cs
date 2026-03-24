using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a Jira issue with its transition timeline.
/// </summary>
public sealed record IssueTimeline
{
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
    /// Tries to calculate cumulative work duration until the issue first reaches the target status.
    /// </summary>
    /// <param name="targetStatusName">Target status used as work completion point.</param>
    /// <returns>
    /// Total duration up to the last transition into the target status, or <c>null</c> when the target status
    /// was never reached.
    /// </returns>
    public TimeSpan? TryBuildWorkDuration(StatusName targetStatusName)
    {
        var targetTransitionIndex = Transitions
            .Select(static (transition, index) => (transition, index))
            .Where(item => string.Equals(
                item.transition.To.Value,
                targetStatusName.Value,
                StringComparison.OrdinalIgnoreCase))
            .Select(static item => item.index)
            .DefaultIfEmpty(-1)
            .Max();
        if (targetTransitionIndex < 0)
        {
            return null;
        }

        var duration = Transitions
            .Take(targetTransitionIndex + 1)
            .Aggregate(TimeSpan.Zero, static (sum, transition) => sum + transition.SincePrevious);

        return duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }
}
