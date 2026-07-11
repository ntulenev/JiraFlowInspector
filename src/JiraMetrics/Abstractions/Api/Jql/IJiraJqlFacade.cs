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
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="createdAfter">The <paramref name="createdAfter"/> value.</param>
    JqlQuery BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter);

    /// <summary>
    /// Builds the query for issues created in the configured month.
    /// </summary>
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    JqlQuery BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for issues moved to a target status in the configured month.
    /// </summary>
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    JqlQuery BuildMovedToDoneIssuesQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for grouped status counts excluding terminal statuses.
    /// </summary>
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="rejectStatusName">The <paramref name="rejectStatusName"/> value.</param>
    JqlQuery BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    /// <summary>
    /// Builds the release issue search query.
    /// </summary>
    /// <param name="request">The <paramref name="request"/> value.</param>
    JqlQuery BuildReleaseIssuesQuery(ReleaseIssueReadRequest request);

    /// <summary>
    /// Builds the architecture tasks search query.
    /// </summary>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    JqlQuery BuildArchTasksQuery(ArchTasksReportSettings settings);

    /// <summary>
    /// Builds the global incidents search query.
    /// </summary>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="incidentStartFields">The <paramref name="incidentStartFields"/> value.</param>
    JqlQuery BuildGlobalIncidentsQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields);
}

