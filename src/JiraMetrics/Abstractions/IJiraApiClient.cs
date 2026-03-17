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
    /// Loads issue keys moved to done this month.
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
    /// Loads count of issues created during configured month and matching issue types.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="issueTypes">Issue types to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching issue count.</returns>
    Task<ItemCount> GetIssueCountCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads count of issues moved to done during configured month and matching issue types.
    /// Created date is not restricted.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="doneStatusName">Done status.</param>
    /// <param name="issueTypes">Issue types to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching issue count.</returns>
    Task<ItemCount> GetIssueCountMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
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
    /// Loads release issues for configured month.
    /// </summary>
    /// <param name="releaseProjectKey">Release project key.</param>
    /// <param name="projectLabel">Project label filter.</param>
    /// <param name="releaseDateFieldName">Release date field name.</param>
    /// <param name="componentsFieldName">Optional components field name.</param>
    /// <param name="hotFixRules">Hot-fix marker rules in format <c>field name -&gt; values</c>.</param>
    /// <param name="rollbackFieldName">Rollback field name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Release issues in selected month.</returns>
    Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads global incidents for configured month.
    /// </summary>
    /// <param name="settings">Global incidents report settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Incident issues in selected month.</returns>
    Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads full issue timeline for a specific issue key.
    /// </summary>
    /// <param name="issueKey">Issue key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue timeline.</returns>
    Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken);
}
