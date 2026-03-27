using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API;

internal sealed class JiraIssueSearchClient : IJiraIssueSearchClient
{
    private readonly IJiraSearchExecutor _searchExecutor;
    private readonly IJiraJqlFacade _jqlFacade;
    private readonly IJiraMapperFacade _mapperFacade;

    public JiraIssueSearchClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IJiraMapperFacade mapperFacade)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        ArgumentNullException.ThrowIfNull(jqlFacade);
        ArgumentNullException.ThrowIfNull(mapperFacade);

        _searchExecutor = searchExecutor;
        _jqlFacade = jqlFacade;
        _mapperFacade = mapperFacade;
    }

    public async Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken)
    {
        var jql = _jqlFacade.BuildMovedToDoneIssueKeysQuery(projectKey, doneStatusName, createdAfter);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["key"], cancellationToken)
            .ConfigureAwait(false);
        return _mapperFacade.MapIssueKeys(issues);
    }

    public async Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        var jql = _jqlFacade.BuildCreatedIssuesQuery(projectKey, issueTypes);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["key", "summary", "created"], cancellationToken)
            .ConfigureAwait(false);
        return _mapperFacade.MapIssueListItems(issues);
    }

    public async Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        var jql = _jqlFacade.BuildMovedToDoneIssuesQuery(projectKey, doneStatusName, issueTypes);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["key", "summary", "created"], cancellationToken)
            .ConfigureAwait(false);
        return _mapperFacade.MapIssueListItems(issues);
    }

    public async Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        CancellationToken cancellationToken)
    {
        var jql = _jqlFacade.BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
            projectKey,
            doneStatusName,
            rejectStatusName);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["status", "issuetype"], cancellationToken)
            .ConfigureAwait(false);
        return _mapperFacade.MapStatusIssueTypeSummaries(issues);
    }
}

