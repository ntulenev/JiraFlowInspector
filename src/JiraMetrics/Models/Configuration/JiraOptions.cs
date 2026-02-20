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
    /// Gets or sets optional rejected status name.
    /// </summary>
    public string? RejectStatusName { get; init; }

    /// <summary>
    /// Gets or sets issue transition settings.
    /// </summary>
    [Required]
    public required IssueTransitionsOptions IssueTransitions { get; init; }

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
    /// Gets or sets optional bug ratio settings.
    /// </summary>
    public BugRatioOptions? BugRatio { get; init; }

    /// <summary>
    /// Gets or sets optional release report settings.
    /// </summary>
    public ReleaseReportOptions? ReleaseReport { get; init; }

    /// <summary>
    /// Gets or sets optional custom field name for filtering.
    /// </summary>
    public string? CustomFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional custom field value for filtering.
    /// </summary>
    public string? CustomFieldValue { get; init; }
}

/// <summary>
/// Issue transition-related options.
/// </summary>
public sealed class IssueTransitionsOptions
{
    /// <summary>
    /// Gets or sets required stage names in path.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> RequiredPathStages { get; init; }

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

/// <summary>
/// Bug ratio options.
/// </summary>
public sealed class BugRatioOptions
{
    /// <summary>
    /// Gets or sets optional issue types that should be treated as bug-like issues.
    /// </summary>
    public IReadOnlyList<string>? BugIssueNames { get; init; }
}

/// <summary>
/// Release report options.
/// </summary>
public sealed class ReleaseReportOptions
{
    /// <summary>
    /// Gets or sets release project key.
    /// </summary>
    public string? ReleaseProjectKey { get; init; }

    /// <summary>
    /// Gets or sets project label used in release search.
    /// </summary>
    public string? ProjectLabel { get; init; }

    /// <summary>
    /// Gets or sets release date field name.
    /// </summary>
    public string? ReleaseDateFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional components field name.
    /// </summary>
    public string? ComponentsFieldName { get; init; }
}
