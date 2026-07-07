namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Configurable status transition measurement rule.
/// </summary>
public sealed class TransitionMeasurementRuleOptions
{
    /// <summary>
    /// Gets or sets source status name.
    /// </summary>
    public string? FromStatusName { get; init; }

    /// <summary>
    /// Gets or sets destination status name.
    /// </summary>
    public string? ToStatusName { get; init; }
}
