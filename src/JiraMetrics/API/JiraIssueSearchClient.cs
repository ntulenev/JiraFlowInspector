using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API;

/// <summary>
/// Executes issue-list searches and maps their results into application models.
/// </summary>
internal sealed class JiraIssueSearchClient : IJiraIssueSearchClient
{

    public JiraIssueSearchClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IJiraFieldResolver fieldResolver)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        ArgumentNullException.ThrowIfNull(jqlFacade);
        ArgumentNullException.ThrowIfNull(fieldResolver);

        _searchExecutor = searchExecutor;
        _jqlFacade = jqlFacade;
        _fieldResolver = fieldResolver;
    }

    public async Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken)
    {
        var jql = _jqlFacade.BuildMovedToDoneIssueKeysQuery(projectKey, doneStatusName, createdAfter);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, JiraSearchFields.From("key"), cancellationToken)
            .ConfigureAwait(false);
        return JiraSearchIssueMapper.ToIssueKeys(issues);
    }

    public async Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken,
        JiraFieldName? reporducedOnProdFieldName = null)
    {
        var jql = _jqlFacade.BuildCreatedIssuesQuery(projectKey, issueTypes);
        var context = await CreateIssueListMappingContextAsync(reporducedOnProdFieldName, cancellationToken)
            .ConfigureAwait(false);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                BuildIssueListRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);
        return JiraSearchIssueMapper.ToIssueListItems(issues, context);
    }

    public async Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken,
        JiraFieldName? reporducedOnProdFieldName = null,
        bool includeIssueLinks = false)
    {
        var jql = _jqlFacade.BuildMovedToDoneIssuesQuery(projectKey, doneStatusName, issueTypes);
        var context = await CreateIssueListMappingContextAsync(
                reporducedOnProdFieldName,
                cancellationToken,
                includeIssueLinks)
            .ConfigureAwait(false);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                BuildIssueListRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);
        return JiraSearchIssueMapper.ToIssueListItems(issues, context);
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
            .SearchIssuesAsync(jql, JiraSearchFields.From("status", "issuetype"), cancellationToken)
            .ConfigureAwait(false);
        return JiraSearchIssueMapper.ToStatusIssueTypeSummaries(issues);
    }

    private async Task<IssueListMappingContext?> CreateIssueListMappingContextAsync(
        JiraFieldName? reporducedOnProdFieldName,
        CancellationToken cancellationToken,
        bool includeIssueLinks = false)
    {
        if (reporducedOnProdFieldName is null && !includeIssueLinks)
        {
            return null;
        }

        if (reporducedOnProdFieldName is null)
        {
            return new IssueListMappingContext(null, null, includeIssueLinks);
        }

        var fieldId = await _fieldResolver
            .TryResolveFieldIdAsync(reporducedOnProdFieldName, cancellationToken)
            .ConfigureAwait(false);
        return new IssueListMappingContext(fieldId, reporducedOnProdFieldName, includeIssueLinks);
    }

    private static JiraSearchFields BuildIssueListRequestedFields(IssueListMappingContext? context)
    {
        var fields = new List<string> { "key", "summary", "created", "priority" };
        if (context?.ReporducedOnProdFieldId is { } fieldId)
        {
            fields.Add(fieldId.Value);
        }
        else if (context?.ReporducedOnProdFieldName is { } fieldName)
        {
            fields.Add(fieldName.Value);
        }

        if (context?.IncludeIssueLinks == true)
        {
            fields.Add("issuelinks");
        }

        return JiraSearchFields.From([.. fields]);
    }
    private readonly IJiraSearchExecutor _searchExecutor;
    private readonly IJiraJqlFacade _jqlFacade;
    private readonly IJiraFieldResolver _fieldResolver;
}

