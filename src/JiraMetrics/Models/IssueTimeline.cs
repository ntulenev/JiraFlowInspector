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
    /// <exception cref="ArgumentException">Thrown when <paramref name="endTime"/> is earlier than <paramref name="created"/>.</exception>
    public IssueTimeline(
        IssueKey key,
        IssueTypeName issueType,
        IssueSummary summary,
        DateTimeOffset created,
        DateTimeOffset endTime,
        IReadOnlyList<TransitionEvent> transitions,
        PathKey pathKey,
        PathLabel pathLabel)
    {
        Key = key;
        IssueType = issueType;
        Summary = summary;
        Created = created;
        EndTime = endTime;
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
        PathKey = pathKey;
        PathLabel = pathLabel;

        if (EndTime < Created)
        {
            throw new ArgumentException("End time cannot be earlier than created time.", nameof(endTime));
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
}
