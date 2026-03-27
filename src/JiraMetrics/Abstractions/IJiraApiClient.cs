using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Jira API client for retrieving issue and user data.
/// </summary>
public interface IJiraApiClient
{
    /// <summary>
    /// Loads authenticated Jira user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated user information.</returns>
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue keys moved to done during the configured report period.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="doneStatusName">Done status.</param>
    /// <param name="createdAfter">Optional lower bound for issue creation date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue keys.</returns>
    Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issues created during configured month and matching issue types.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="issueTypes">Issue types to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching issues with key and title.</returns>
    Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issues moved to done during configured month and matching issue types.
    /// Created date is not restricted.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="doneStatusName">Done status.</param>
    /// <param name="issueTypes">Issue types to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching issues with key and title.</returns>
    Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue counts grouped by status and issue type, excluding done/rejected statuses.
    /// Issue type filter from settings is not applied.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="doneStatusName">Done status to exclude.</param>
    /// <param name="rejectStatusName">Optional reject status to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue counts grouped by status and issue type.</returns>
    Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads release issues for the configured report period.
    /// </summary>
    /// <param name="releaseProjectKey">Release project key.</param>
    /// <param name="projectLabel">Project label filter.</param>
    /// <param name="releaseDateFieldName">Release date field name.</param>
    /// <param name="componentsFieldName">Optional components field name.</param>
    /// <param name="hotFixRules">Hot-fix marker rules in format <c>field name -&gt; values</c>.</param>
    /// <param name="rollbackFieldName">Rollback field name.</param>
    /// <param name="environmentFieldName">Optional environment field name used for filtering.</param>
    /// <param name="environmentFieldValue">Optional environment field value used for filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Release issues in selected period.</returns>
    Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads architecture tasks for configured query.
    /// </summary>
    /// <param name="settings">Architecture tasks report settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Architecture task rows.</returns>
    Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads global incidents for the configured report period.
    /// </summary>
    /// <param name="settings">Global incidents report settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Incident issues in selected period.</returns>
    Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue timelines in batches.
    /// </summary>
    /// <param name="issueKeys">Issue keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded issues and per-key failures.</returns>
    Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads full issue timeline for a specific issue key.
    /// </summary>
    /// <param name="issueKey">Issue key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue timeline.</returns>
    Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken);
}
