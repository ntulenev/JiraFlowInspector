using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated settings for a dedicated transition-duration analysis section.
/// </summary>
public sealed record CustomTransitionAnalysisSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomTransitionAnalysisSettings"/> class.
    /// </summary>
    /// <param name="fromStatusName">Source status name.</param>
    /// <param name="toStatusName">Destination status name.</param>
    /// <param name="codeOnly">Whether only issues with code artifacts should be shown.</param>
    public CustomTransitionAnalysisSettings(
        StatusName fromStatusName,
        StatusName toStatusName,
        bool codeOnly = false)
    {
        FromStatusName = fromStatusName;
        ToStatusName = toStatusName;
        CodeOnly = codeOnly;
    }

    /// <summary>
    /// Gets source status name.
    /// </summary>
    public StatusName FromStatusName { get; }

    /// <summary>
    /// Gets destination status name.
    /// </summary>
    public StatusName ToStatusName { get; }

    /// <summary>
    /// Gets a value indicating whether only issues with code artifacts should be shown.
    /// </summary>
    public bool CodeOnly { get; }

    /// <summary>
    /// Gets human-readable transition label.
    /// </summary>
    public string Label => $"{FromStatusName.Value} -> {ToStatusName.Value}";
}
