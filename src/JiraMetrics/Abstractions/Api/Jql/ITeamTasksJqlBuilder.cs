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
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="createdAfter">The <paramref name="createdAfter"/> value.</param>
    /// <returns>The result of the operation.</returns>
    JqlQuery BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter);

    /// <summary>
    /// Builds the query for issues created in the configured month.
    /// </summary>
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    /// <returns>The result of the operation.</returns>
    JqlQuery BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes);

    /// <summary>
    /// Builds the query for issues moved to a target status in the configured month.
    /// </summary>
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    /// <returns>The result of the operation.</returns>
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
    /// <returns>The result of the operation.</returns>
    JqlQuery BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName);
}

