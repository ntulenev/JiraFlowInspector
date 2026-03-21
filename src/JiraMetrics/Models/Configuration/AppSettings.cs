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
    /// <param name="rejectStatusName">Optional reject status name.</param>
    /// <param name="requiredPathStages">Required path stages.</param>
    /// <param name="monthLabel">Month label used in output.</param>
    /// <param name="createdAfter">Optional lower bound for issue creation date.</param>
    /// <param name="issueTypes">Optional issue types filter.</param>
    /// <param name="customFieldName">Optional custom field name for filtering.</param>
    /// <param name="customFieldValue">Optional custom field value for filtering.</param>
    /// <param name="showTimeCalculationsInHoursOnly">Whether all time calculations should be shown strictly in hours.</param>
    /// <param name="excludeWeekend">Whether to exclude weekends from transition durations.</param>
    /// <param name="excludedDays">Optional list of excluded days.</param>
    /// <param name="bugIssueNames">Optional issue types that should be treated as bug-like issues.</param>
    /// <param name="showGeneralStatistics">Whether to show general statistics section.</param>
    /// <param name="releaseReport">Optional release report settings.</param>
    /// <param name="globalIncidentsReport">Optional global incidents report settings.</param>
    /// <param name="pdfReport">PDF report settings.</param>
    /// <param name="pullRequestFieldName">Pull request field name or id used for code-activity detection.</param>
    public AppSettings(
        JiraBaseUrl baseUrl,
        JiraEmail email,
        JiraApiToken apiToken,
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        IReadOnlyList<StageName> requiredPathStages,
        MonthLabel monthLabel,
        CreatedAfterDate? createdAfter = null,
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        string? customFieldName = null,
        string? customFieldValue = null,
        bool showTimeCalculationsInHoursOnly = false,
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null,
        IReadOnlyList<IssueTypeName>? bugIssueNames = null,
        bool showGeneralStatistics = true,
        ReleaseReportSettings? releaseReport = null,
        GlobalIncidentsReportSettings? globalIncidentsReport = null,
        PdfReportSettings? pdfReport = null,
        string? pullRequestFieldName = null)
    {
        BaseUrl = baseUrl;
        Email = email;
        ApiToken = apiToken;
        ProjectKey = projectKey;
        DoneStatusName = doneStatusName;
        RejectStatusName = rejectStatusName;
        RequiredPathStages = requiredPathStages is null ? [] : [.. requiredPathStages];
        MonthLabel = monthLabel;
        CreatedAfter = createdAfter;
        IssueTypes = issueTypes is null ? [] : [.. issueTypes];
        CustomFieldName = string.IsNullOrWhiteSpace(customFieldName) ? null : customFieldName.Trim();
        CustomFieldValue = string.IsNullOrWhiteSpace(customFieldValue) ? null : customFieldValue.Trim();
        ShowTimeCalculationsInHoursOnly = showTimeCalculationsInHoursOnly;
        ExcludeWeekend = excludeWeekend;
        ExcludedDays = excludedDays is null ? [] : [.. excludedDays];
        BugIssueNames = bugIssueNames is null ? [] : [.. bugIssueNames];
        ShowGeneralStatistics = showGeneralStatistics;
        ReleaseReport = releaseReport;
        GlobalIncidentsReport = globalIncidentsReport;
        PdfReport = pdfReport ?? new PdfReportSettings();
        PullRequestFieldName = string.IsNullOrWhiteSpace(pullRequestFieldName)
            ? null!
            : pullRequestFieldName.Trim();
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
    /// Gets optional reject status name.
    /// </summary>
    public StatusName? RejectStatusName { get; }

    /// <summary>
    /// Gets required path stages.
    /// </summary>
    public IReadOnlyList<StageName> RequiredPathStages { get; }

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
    /// Gets optional issue types that should be treated as bug-like issues.
    /// </summary>
    public IReadOnlyList<IssueTypeName> BugIssueNames { get; }

    /// <summary>
    /// Gets whether to show general statistics section.
    /// </summary>
    public bool ShowGeneralStatistics { get; }

    /// <summary>
    /// Gets optional release report settings.
    /// </summary>
    public ReleaseReportSettings? ReleaseReport { get; }

    /// <summary>
    /// Gets optional global incidents report settings.
    /// </summary>
    public GlobalIncidentsReportSettings? GlobalIncidentsReport { get; }

    /// <summary>
    /// Gets PDF report settings.
    /// </summary>
    public PdfReportSettings PdfReport { get; }

    /// <summary>
    /// Gets optional custom field name for filtering.
    /// </summary>
    public string? CustomFieldName { get; }

    /// <summary>
    /// Gets optional custom field value for filtering.
    /// </summary>
    public string? CustomFieldValue { get; }

    /// <summary>
    /// Gets whether all time calculations should be shown strictly in hours.
    /// </summary>
    public bool ShowTimeCalculationsInHoursOnly { get; }

    /// <summary>
    /// Gets whether to exclude weekends from transition durations.
    /// </summary>
    public bool ExcludeWeekend { get; }

    /// <summary>
    /// Gets optional list of excluded days.
    /// </summary>
    public IReadOnlyList<DateOnly> ExcludedDays { get; }

    /// <summary>
    /// Gets pull request field name or id used for code-activity detection.
    /// </summary>
    public string PullRequestFieldName { get; }
}
