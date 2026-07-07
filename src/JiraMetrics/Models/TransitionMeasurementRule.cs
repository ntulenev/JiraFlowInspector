using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a status transition pattern used by transition measurements.
/// </summary>
public sealed record TransitionMeasurementRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionMeasurementRule"/> class.
    /// </summary>
    /// <param name="fromStatusName">Source status.</param>
    /// <param name="toStatusName">Destination status.</param>
    public TransitionMeasurementRule(StatusName fromStatusName, StatusName toStatusName)
    {
        FromStatusName = fromStatusName;
        ToStatusName = toStatusName;
    }

    /// <summary>
    /// Gets source status.
    /// </summary>
    public StatusName FromStatusName { get; }

    /// <summary>
    /// Gets destination status.
    /// </summary>
    public StatusName ToStatusName { get; }

    /// <summary>
    /// Gets human-readable rule label.
    /// </summary>
    public string Label => $"{FromStatusName.Value} -> {ToStatusName.Value}";
}
