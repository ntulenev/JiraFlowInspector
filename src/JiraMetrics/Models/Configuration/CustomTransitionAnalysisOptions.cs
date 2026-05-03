using System.ComponentModel.DataAnnotations;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Optional settings for a dedicated transition-duration analysis section.
/// </summary>
public sealed class CustomTransitionAnalysisOptions
{
    /// <summary>
    /// Gets or sets source status name.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string FromStatusName { get; init; }

    /// <summary>
    /// Gets or sets destination status name.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string ToStatusName { get; init; }

    /// <summary>
    /// Gets or sets whether only issues with code artifacts should be shown.
    /// </summary>
    public bool CodeOnly { get; init; }

    /// <summary>
    /// Gets or sets whether a separate PDF report should be generated for this analysis.
    /// </summary>
    public bool GenerateSeparateReport { get; init; }
}
