using System.ComponentModel.DataAnnotations;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Team tasks options.
/// </summary>
public sealed class TeamTasksOptions
{
    /// <summary>
    /// Gets or sets Jira project key.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string ProjectKey { get; init; }

    /// <summary>
    /// Gets or sets done status name.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string DoneStatusName { get; init; }

    /// <summary>
    /// Gets or sets optional rejected status name.
    /// </summary>
    public string? RejectStatusName { get; init; }

    /// <summary>
    /// Gets or sets issue transition settings.
    /// </summary>
    [Required]
    public required IssueTransitionsOptions IssueTransitions { get; init; }

    /// <summary>
    /// Gets or sets optional bug ratio settings.
    /// </summary>
    public BugRatioOptions? BugRatio { get; init; }

    /// <summary>
    /// Gets or sets optional custom field name for filtering.
    /// </summary>
    public string? CustomFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional custom field value for filtering.
    /// </summary>
    public string? CustomFieldValue { get; init; }

    /// <summary>
    /// Gets or sets whether general statistics should be shown.
    /// </summary>
    public bool ShowGeneralStatistics { get; init; } = true;
}
