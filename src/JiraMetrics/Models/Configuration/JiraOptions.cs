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
    /// Gets or sets team-tasks analytics settings.
    /// </summary>
    [Required]
    public required TeamTasksOptions TeamTasks { get; init; }

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
    /// Gets or sets whether all time calculations should be shown strictly in hours.
    /// </summary>
    public bool ShowTimeCalculationsInHoursOnly { get; init; }

    /// <summary>
    /// Gets or sets number of retries for transient Jira API errors.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets or sets pull request field name or id used for code-activity detection.
    /// </summary>
    public string? PullRequestFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional release report settings.
    /// </summary>
    public ReleaseReportOptions? ReleaseReport { get; init; }

    /// <summary>
    /// Gets or sets optional global incidents report settings.
    /// </summary>
    public GlobalIncidentsReportOptions? GlobalIncidents { get; init; }

    /// <summary>
    /// Gets or sets PDF report settings.
    /// </summary>
    public PdfOptions Pdf { get; init; } = new();
}

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

    /// <summary>
    /// Gets or sets optional hot-fix marker rules in format <c>field name -&gt; values</c>.
    /// </summary>
    public Dictionary<string, string[]>? HotFixRules { get; init; }

    /// <summary>
    /// Gets or sets optional rollback field name.
    /// </summary>
    public string? RollbackFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional environment field name used for release filtering.
    /// </summary>
    public string? EnvironmentFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional environment field value used for release filtering.
    /// </summary>
    public string? EnvironmentFieldValue { get; init; }
}

/// <summary>
/// Global incidents report options.
/// </summary>
public sealed class GlobalIncidentsReportOptions
{
    /// <summary>
    /// Gets or sets Jira namespace or project used for incidents search.
    /// </summary>
    public string Namespace { get; init; } = "Incidents";

    /// <summary>
    /// Gets or sets optional raw JQL clause used to filter incidents.
    /// </summary>
    public string? JqlFilter { get; init; }

    /// <summary>
    /// Gets or sets optional free-text phrase used to filter incidents.
    /// </summary>
    public string? SearchPhrase { get; init; }

    /// <summary>
    /// Gets or sets incident start field name.
    /// </summary>
    public string IncidentStartFieldName { get; init; } = "Incident Start date/time UTC";

    /// <summary>
    /// Gets or sets incident recovery field name.
    /// </summary>
    public string IncidentRecoveryFieldName { get; init; } = "Incident Recovery date/time UTC";

    /// <summary>
    /// Gets or sets impact field name.
    /// </summary>
    public string ImpactFieldName { get; init; } = "Impact";

    /// <summary>
    /// Gets or sets urgency field name.
    /// </summary>
    public string UrgencyFieldName { get; init; } = "Urgency";

    /// <summary>
    /// Gets or sets optional additional field names shown in the report.
    /// </summary>
    public IReadOnlyList<string>? AdditionalFieldNames { get; init; }
}
