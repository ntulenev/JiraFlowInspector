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
}

