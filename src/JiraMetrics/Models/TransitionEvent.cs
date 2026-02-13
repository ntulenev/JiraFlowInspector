using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a single status transition event.
/// </summary>
public sealed record TransitionEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionEvent"/> class.
    /// </summary>
    /// <param name="from">Source status.</param>
    /// <param name="to">Destination status.</param>
    /// <param name="at">Transition timestamp.</param>
    /// <param name="sincePrevious">Duration since previous transition.</param>
    public TransitionEvent(StatusName from, StatusName to, DateTimeOffset at, TimeSpan sincePrevious)
    {
        From = from;
        To = to;
        At = at;
        SincePrevious = sincePrevious;
    }

    /// <summary>
    /// Gets source status.
    /// </summary>
    public StatusName From { get; }

    /// <summary>
    /// Gets destination status.
    /// </summary>
    public StatusName To { get; }

    /// <summary>
    /// Gets transition timestamp.
    /// </summary>
    public DateTimeOffset At { get; }

    /// <summary>
    /// Gets elapsed duration since previous event.
    /// </summary>
    public TimeSpan SincePrevious { get; }
}
