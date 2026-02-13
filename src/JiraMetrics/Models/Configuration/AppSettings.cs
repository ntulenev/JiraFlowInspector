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
    public AppSettings(
        JiraBaseUrl baseUrl,
        JiraEmail email,
        JiraApiToken apiToken,
        ProjectKey projectKey,
        StatusName doneStatusName,
        StageName requiredPathStage,
        MonthLabel monthLabel,
        CreatedAfterDate? createdAfter = null)
    {
        BaseUrl = baseUrl;
        Email = email;
        ApiToken = apiToken;
        ProjectKey = projectKey;
        DoneStatusName = doneStatusName;
        RequiredPathStage = requiredPathStage;
        MonthLabel = monthLabel;
        CreatedAfter = createdAfter;
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
}
