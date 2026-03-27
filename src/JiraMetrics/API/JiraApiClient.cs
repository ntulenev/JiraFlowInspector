using JiraMetrics.Abstractions;
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
    private readonly IJiraUserClient _userClient;
    private readonly IJiraIssueSearchClient _issueSearchClient;
    private readonly IJiraReportDataClient _reportDataClient;
    private readonly IJiraIssueTimelineClient _issueTimelineClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiClient"/> class.
    /// </summary>
    public JiraApiClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IOptions<AppSettings> settings,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapperFacade)
        : this(
            new JiraUserClient(searchExecutor),
            new JiraIssueSearchClient(searchExecutor, jqlFacade, mapperFacade),
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
    public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _userClient.GetCurrentUserAsync(cancellationToken);

    /// <inheritdoc />
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
    public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken) =>
        _issueSearchClient.GetIssuesCreatedThisMonthAsync(projectKey, issueTypes, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken) =>
        _issueSearchClient.GetIssuesMovedToDoneThisMonthAsync(
            projectKey,
            doneStatusName,
            issueTypes,
            cancellationToken);

    /// <inheritdoc />
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
    public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetReleaseIssuesForMonthAsync(
            releaseProjectKey,
            projectLabel,
            releaseDateFieldName,
            componentsFieldName,
            hotFixRules,
            rollbackFieldName,
            environmentFieldName,
            environmentFieldValue,
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetArchTasksAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken) =>
        _reportDataClient.GetGlobalIncidentsForMonthAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken) =>
        _issueTimelineClient.GetIssueTimelineAsync(issueKey, cancellationToken);

    /// <inheritdoc />
    public Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken) =>
        _issueTimelineClient.GetIssueTimelinesAsync(issueKeys, cancellationToken);
}
