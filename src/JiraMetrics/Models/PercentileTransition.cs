using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents percentile duration for a status transition.
/// </summary>
public sealed record PercentileTransition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PercentileTransition"/> class.
    /// </summary>
    /// <param name="from">Source status.</param>
    /// <param name="to">Destination status.</param>
    /// <param name="p75Duration">P75 duration.</param>
    public PercentileTransition(StatusName from, StatusName to, TimeSpan p75Duration)
    {
        From = from;
        To = to;
        P75Duration = p75Duration;
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
    /// Gets P75 duration.
    /// </summary>
    public TimeSpan P75Duration { get; }
}
