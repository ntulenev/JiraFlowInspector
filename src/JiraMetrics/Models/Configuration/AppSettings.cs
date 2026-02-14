using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated application settings mapped from configuration.
/// </summary>
public sealed record AppSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettings"/> class.
    /// </summary>
    /// <param name="baseUrl">Jira base url.</param>
    /// <param name="email">Jira account email.</param>
    /// <param name="apiToken">Jira API token.</param>
    /// <param name="projectKey">Jira project key.</param>
    /// <param name="doneStatusName">Done status name.</param>
    /// <param name="requiredPathStage">Required path stage.</param>
    /// <param name="monthLabel">Month label used in output.</param>
    /// <param name="createdAfter">Optional lower bound for issue creation date.</param>
    /// <param name="issueTypes">Optional issue types filter.</param>
    /// <param name="excludeWeekend">Whether to exclude weekends from transition durations.</param>
    /// <param name="excludedDays">Optional list of excluded days.</param>
    public AppSettings(
        JiraBaseUrl baseUrl,
        JiraEmail email,
        JiraApiToken apiToken,
        ProjectKey projectKey,
        StatusName doneStatusName,
        StageName requiredPathStage,
        MonthLabel monthLabel,
        CreatedAfterDate? createdAfter = null,
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null)
    {
        BaseUrl = baseUrl;
        Email = email;
        ApiToken = apiToken;
        ProjectKey = projectKey;
        DoneStatusName = doneStatusName;
        RequiredPathStage = requiredPathStage;
        MonthLabel = monthLabel;
        CreatedAfter = createdAfter;
        IssueTypes = issueTypes is null ? [] : [.. issueTypes];
        ExcludeWeekend = excludeWeekend;
        ExcludedDays = excludedDays is null ? [] : [.. excludedDays];
    }

    /// <summary>
    /// Gets Jira base URL.
    /// </summary>
    public JiraBaseUrl BaseUrl { get; }

    /// <summary>
    /// Gets Jira account email.
    /// </summary>
    public JiraEmail Email { get; }

    /// <summary>
    /// Gets Jira API token.
    /// </summary>
    public JiraApiToken ApiToken { get; }

    /// <summary>
    /// Gets Jira project key.
    /// </summary>
    public ProjectKey ProjectKey { get; }

    /// <summary>
    /// Gets done status name.
    /// </summary>
    public StatusName DoneStatusName { get; }

    /// <summary>
    /// Gets required path stage.
    /// </summary>
    public StageName RequiredPathStage { get; }

    /// <summary>
    /// Gets month label.
    /// </summary>
    public MonthLabel MonthLabel { get; }

    /// <summary>
    /// Gets optional lower bound for issue creation date.
    /// </summary>
    public CreatedAfterDate? CreatedAfter { get; }

    /// <summary>
    /// Gets optional issue types filter.
    /// </summary>
    public IReadOnlyList<IssueTypeName> IssueTypes { get; }

    /// <summary>
    /// Gets whether to exclude weekends from transition durations.
    /// </summary>
    public bool ExcludeWeekend { get; }

    /// <summary>
    /// Gets optional list of excluded days.
    /// </summary>
    public IReadOnlyList<DateOnly> ExcludedDays { get; }
}
