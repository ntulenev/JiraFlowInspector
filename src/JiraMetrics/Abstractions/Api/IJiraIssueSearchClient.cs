using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api;

internal interface IJiraIssueSearchClient
{
    Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        CancellationToken cancellationToken);
}

