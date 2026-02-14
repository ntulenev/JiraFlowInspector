using System.ComponentModel.DataAnnotations;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Raw configuration options bound from the <c>Jira</c> section in <c>appsettings.json</c>.
/// </summary>
public sealed class JiraOptions
{
    /// <summary>
    /// Gets or sets Jira base URL.
    /// </summary>
    [Required]
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Gets or sets Jira account email.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets Jira API token.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string ApiToken { get; init; }

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
    /// Gets or sets required stage name in path.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string RequiredPathStage { get; init; }

    /// <summary>
    /// Gets or sets month label.
    /// </summary>
    [RegularExpression(@"^\d{4}-\d{2}$")]
    public string? MonthLabel { get; init; }

    /// <summary>
    /// Gets or sets optional lower bound for issue creation date in yyyy-MM-dd format.
    /// </summary>
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
    public string? CreatedAfter { get; init; }

    /// <summary>
    /// Gets or sets number of retries for transient Jira API errors.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets or sets optional issue types filter.
    /// </summary>
    public IReadOnlyList<string>? IssueTypes { get; init; }

    /// <summary>
    /// Gets or sets whether to exclude weekends from transition durations.
    /// </summary>
    public bool ExcludeWeekend { get; init; }

    /// <summary>
    /// Gets or sets optional list of excluded days in dd.MM.yyyy or yyyy-MM-dd format.
    /// </summary>
    public IReadOnlyList<string>? ExcludedDays { get; init; }
}
