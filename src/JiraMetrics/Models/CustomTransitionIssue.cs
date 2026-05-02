namespace JiraMetrics.Models;

/// <summary>
/// Represents an issue matched by a configured status transition.
/// </summary>
public sealed record CustomTransitionIssue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTransitionIssue"/> class.
    /// </summary>
    /// <param name="issue">Issue timeline.</param>
    /// <param name="transitionAt">Transition timestamp.</param>
    /// <param name="duration">Transition duration.</param>
    public CustomTransitionIssue(IssueTimeline issue, DateTimeOffset transitionAt, TimeSpan duration)
    {
        Issue = issue ?? throw new ArgumentNullException(nameof(issue));
        TransitionAt = transitionAt;
        Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    /// <summary>
    /// Gets issue timeline.
    /// </summary>
    public IssueTimeline Issue { get; }

    /// <summary>
    /// Gets matching transition timestamp.
    /// </summary>
    public DateTimeOffset TransitionAt { get; }

    /// <summary>
    /// Gets matching transition duration.
    /// </summary>
    public TimeSpan Duration { get; }
}
