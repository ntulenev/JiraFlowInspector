using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api.Jql;

/// <summary>
/// Builds JQL queries for team-task reads.
/// </summary>
public interface ITeamTasksJqlBuilder
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
}

