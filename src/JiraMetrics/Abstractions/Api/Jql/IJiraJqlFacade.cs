using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api.Jql;

/// <summary>
/// Facade for building Jira JQL queries used by the API client.
/// </summary>
public interface IJiraJqlFacade
{
    /// <summary>
    /// Builds the query for keys moved to a target status in the configured month.
    /// </summary>
    JqlQuery BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter);

    /// <summary>
    /// Builds the query for issues created in the configured month.
    /// </summary>
    JqlQuery BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for issues moved to a target status in the configured month.
    /// </summary>
    JqlQuery BuildMovedToDoneIssuesQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for grouped status counts excluding terminal statuses.
    /// </summary>
    JqlQuery BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    /// <summary>
    /// Builds the release issue search query.
    /// </summary>
    JqlQuery BuildReleaseIssuesQuery(ReleaseIssueReadRequest request);

    /// <summary>
    /// Builds the architecture tasks search query.
    /// </summary>
    JqlQuery BuildArchTasksQuery(ArchTasksReportSettings settings);

    /// <summary>
    /// Builds the global incidents search query.
    /// </summary>
    JqlQuery BuildGlobalIncidentsQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields);
}

