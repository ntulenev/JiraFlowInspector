using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.API;

/// <summary>
/// Compatibility facade over focused Jira API clients.
/// </summary>
public sealed class JiraApiClient : IJiraApiClient
{

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiClient"/> class.
    /// </summary>
    /// <param name="searchExecutor">The <paramref name="searchExecutor"/> value.</param>
    /// <param name="jqlFacade">The <paramref name="jqlFacade"/> value.</param>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="fieldResolver">The <paramref name="fieldResolver"/> value.</param>
    /// <param name="mapperFacade">The <paramref name="mapperFacade"/> value.</param>
    public JiraApiClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IOptions<AppSettings> settings,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapperFacade)
        : this(
            new JiraUserClient(searchExecutor),
            new JiraIssueSearchClient(searchExecutor, jqlFacade, fieldResolver, mapperFacade),
            new JiraReportDataClient(searchExecutor, jqlFacade, fieldResolver, mapperFacade),
            new JiraIssueTimelineClient(searchExecutor, settings, fieldResolver, mapperFacade))
    {
    }

    internal JiraApiClient(
        IJiraUserClient userClient,
        IJiraIssueSearchClient issueSearchClient,
        IJiraReportDataClient reportDataClient,
        IJiraIssueTimelineClient issueTimelineClient)
    {
        ArgumentNullException.ThrowIfNull(userClient);
        ArgumentNullException.ThrowIfNull(issueSearchClient);
        ArgumentNullException.ThrowIfNull(reportDataClient);
        ArgumentNullException.ThrowIfNull(issueTimelineClient);

        _userClient = userClient;
        _issueSearchClient = issueSearchClient;
        _reportDataClient = reportDataClient;
        _issueTimelineClient = issueTimelineClient;
    }

    /// <inheritdoc />
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _userClient.GetCurrentUserAsync(cancellationToken);

    /// <inheritdoc />
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="createdAfter">The <paramref name="createdAfter"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken) =>
        _issueSearchClient.GetIssueKeysMovedToDoneThisMonthAsync(
            projectKey,
            doneStatusName,
            createdAfter,
            cancellationToken);

    /// <inheritdoc />
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <param name="reporducedOnProdFieldName">The <paramref name="reporducedOnProdFieldName"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken,
        JiraFieldName? reporducedOnProdFieldName = null) =>
        _issueSearchClient.GetIssuesCreatedThisMonthAsync(
            projectKey,
            issueTypes,
            cancellationToken,
            reporducedOnProdFieldName);

    /// <inheritdoc />
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <param name="reporducedOnProdFieldName">The <paramref name="reporducedOnProdFieldName"/> value.</param>
    /// <param name="includeIssueLinks">The <paramref name="includeIssueLinks"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken,
        JiraFieldName? reporducedOnProdFieldName = null,
        bool includeIssueLinks = false) =>
        _issueSearchClient.GetIssuesMovedToDoneThisMonthAsync(
            projectKey,
            doneStatusName,
            issueTypes,
            cancellationToken,
            reporducedOnProdFieldName,
            includeIssueLinks);

    /// <inheritdoc />
    /// <param name="projectKey">The <paramref name="projectKey"/> value.</param>
    /// <param name="doneStatusName">The <paramref name="doneStatusName"/> value.</param>
    /// <param name="rejectStatusName">The <paramref name="rejectStatusName"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        CancellationToken cancellationToken) =>
        _issueSearchClient.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            projectKey,
            doneStatusName,
            rejectStatusName,
            cancellationToken);

    /// <inheritdoc />
    /// <param name="request">The <paramref name="request"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ReleaseIssueReadRequest request,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetReleaseIssuesForMonthAsync(
            request,
            cancellationToken);

    /// <inheritdoc />
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetArchTasksAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<IssueListItem>> GetUnresolved30DaysTasksAsync(
        Unresolved30DaysTasksReportSettings settings,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetUnresolved30DaysTasksAsync(settings, cancellationToken);

    /// <inheritdoc />
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetGlobalIncidentsForMonthAsync(settings, cancellationToken);

    /// <inheritdoc />
    /// <param name="issueKey">The <paramref name="issueKey"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken) =>
        _issueTimelineClient.GetIssueTimelineAsync(issueKey, cancellationToken);

    /// <inheritdoc />
    /// <param name="issueKeys">The <paramref name="issueKeys"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken) =>
        _issueTimelineClient.GetIssueTimelinesAsync(issueKeys, cancellationToken);
    private readonly IJiraUserClient _userClient;
    private readonly IJiraIssueSearchClient _issueSearchClient;
    private readonly IJiraReportDataClient _reportDataClient;
    private readonly IJiraIssueTimelineClient _issueTimelineClient;
}

