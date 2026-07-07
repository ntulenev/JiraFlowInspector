namespace JiraMetrics.Models;

/// <summary>
/// Represents an issue matched by one of transition measurement rules.
/// </summary>
public sealed record TransitionMeasurementIssue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionMeasurementIssue"/> class.
    /// </summary>
    /// <param name="issue">Issue timeline.</param>
    /// <param name="rule">Matched transition rule.</param>
    /// <param name="transitionAt">Transition timestamp.</param>
    /// <param name="duration">Transition duration.</param>
    public TransitionMeasurementIssue(
        IssueTimeline issue,
        TransitionMeasurementRule rule,
        DateTimeOffset transitionAt,
        TimeSpan duration)
    {
        Issue = issue ?? throw new ArgumentNullException(nameof(issue));
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        TransitionAt = transitionAt;
        Duration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
    }

    /// <summary>
    /// Gets issue timeline.
    /// </summary>
    public IssueTimeline Issue { get; }

    /// <summary>
    /// Gets matched transition rule.
    /// </summary>
    public TransitionMeasurementRule Rule { get; }

    /// <summary>
    /// Gets matching transition timestamp.
    /// </summary>
    public DateTimeOffset TransitionAt { get; }

    /// <summary>
    /// Gets matching transition duration.
    /// </summary>
    public TimeSpan Duration { get; }
}
