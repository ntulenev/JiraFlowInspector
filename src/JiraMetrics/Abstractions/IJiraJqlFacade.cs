using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Facade for building Jira JQL queries used by the API client.
/// </summary>
public interface IJiraJqlFacade
{
    /// <summary>
    /// Builds the query for keys moved to a target status in the configured month.
    /// </summary>
    string BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter);

    /// <summary>
    /// Builds the query for issues created in the configured month.
    /// </summary>
    string BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for issues moved to a target status in the configured month.
    /// </summary>
    string BuildMovedToDoneIssuesQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for grouped status counts excluding terminal statuses.
    /// </summary>
    string BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    /// <summary>
    /// Builds the release issue search query.
    /// </summary>
    string BuildReleaseIssuesQuery(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? environmentFieldName,
        string? environmentFieldValue);

    /// <summary>
    /// Builds the global incidents search query.
    /// </summary>
    string BuildGlobalIncidentsQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields);
}
