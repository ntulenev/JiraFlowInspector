using System.Text.Json;

using JiraMetrics.Abstractions;
using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

namespace JiraMetrics.API;

/// <summary>
/// Jira REST API client implementation.
/// </summary>
public sealed class JiraApiClient : IJiraApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiClient"/> class.
    /// </summary>
    /// <param name="searchExecutor">Search executor.</param>
    /// <param name="jqlFacade">JQL facade.</param>
    /// <param name="settings">Application settings.</param>
    /// <param name="fieldResolver">Field resolver.</param>
    /// <param name="mapperFacade">Mapping facade.</param>
    public JiraApiClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IOptions<AppSettings> settings,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapperFacade)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        ArgumentNullException.ThrowIfNull(jqlFacade);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(fieldResolver);
        ArgumentNullException.ThrowIfNull(mapperFacade);

        _searchExecutor = searchExecutor;
        _jqlFacade = jqlFacade;
        _pullRequestFieldName = settings.Value.PullRequestFieldName;
        _fieldResolver = fieldResolver;
        _mapperFacade = mapperFacade;
    }

    /// <inheritdoc />
    public async Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await _searchExecutor
            .GetCurrentUserAsync(cancellationToken)
            .ConfigureAwait(false);
        if (response is null)
        {
            throw new InvalidOperationException("Jira user response is empty.");
        }

        var displayName =
            response.DisplayName
            ?? response.EmailAddress
            ?? response.AccountId
            ?? "unknown";
        return new JiraAuthUser(new UserDisplayName(displayName), response.EmailAddress, response.AccountId);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<StatusIssueTypeSummary>>
        GetIssueCountsByStatusExcludingDoneAndRejectAsync(
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);
        ArgumentNullException.ThrowIfNull(hotFixRules);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackFieldName);

        var releaseFieldId = await _fieldResolver
            .ResolveFieldIdAsync(releaseDateFieldName, cancellationToken)
            .ConfigureAwait(false);
        var componentsFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(componentsFieldName, cancellationToken)
            .ConfigureAwait(false);
        var resolvedHotFixRules = await _fieldResolver
            .ResolveHotFixRulesAsync(hotFixRules, cancellationToken)
            .ConfigureAwait(false);
        var rollbackFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(rollbackFieldName, cancellationToken)
            .ConfigureAwait(false);
        var environmentFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(environmentFieldName, cancellationToken)
            .ConfigureAwait(false);

        var jql = _jqlFacade.BuildReleaseIssuesQuery(
            releaseProjectKey,
            projectLabel,
            releaseDateFieldName,
            environmentFieldName,
            environmentFieldValue);
        var context = new ReleaseIssueMappingContext(
            releaseFieldId,
            releaseDateFieldName,
            componentsFieldId,
            componentsFieldName,
            resolvedHotFixRules,
            rollbackFieldId,
            rollbackFieldName,
            environmentFieldId,
            environmentFieldName);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                _mapperFacade.BuildReleaseRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapReleaseIssues(issues, context);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var jql = _jqlFacade.BuildArchTasksQuery(settings);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["key", "summary", "created", "resolutiondate"], cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapArchTaskItems(issues);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var startFields = await _fieldResolver.ResolveDateFieldsAsync(
            settings.IncidentStartFieldName,
            settings.IncidentStartFallbackFieldName,
            cancellationToken).ConfigureAwait(false);
        var recoveryFields = await _fieldResolver.ResolveDateFieldsAsync(
            settings.IncidentRecoveryFieldName,
            settings.IncidentRecoveryFallbackFieldName,
            cancellationToken).ConfigureAwait(false);
        var impactFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(settings.ImpactFieldName, cancellationToken)
            .ConfigureAwait(false);
        var urgencyFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(settings.UrgencyFieldName, cancellationToken)
            .ConfigureAwait(false);

        var additionalFieldIds = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var additionalFieldName in settings.AdditionalFieldNames)
        {
            additionalFieldIds[additionalFieldName] = await _fieldResolver
                .TryResolveFieldIdAsync(additionalFieldName, cancellationToken)
                .ConfigureAwait(false);
        }

        var jql = _jqlFacade.BuildGlobalIncidentsQuery(settings, startFields);
        var context = new GlobalIncidentMappingContext(
            startFields,
            recoveryFields,
            impactFieldId,
            settings.ImpactFieldName,
            urgencyFieldId,
            settings.UrgencyFieldName,
            additionalFieldIds);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                _mapperFacade.BuildGlobalIncidentRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapGlobalIncidents(issues, context);
    }

    /// <inheritdoc />
    public async Task<IssueTimeline> GetIssueTimelineAsync(
        IssueKey issueKey,
        CancellationToken cancellationToken)
    {
        var requestedFields = await BuildIssueTimelineRequestedFieldsAsync(cancellationToken)
            .ConfigureAwait(false);
        var response = await _searchExecutor
            .GetIssueWithChangelogAsync(issueKey, requestedFields, cancellationToken)
            .ConfigureAwait(false);
        if (response is null)
        {
            throw new InvalidOperationException("Jira issue response is empty.");
        }

        return _mapperFacade.MapIssueTimeline(response, issueKey);
    }

    /// <inheritdoc />
    public async Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueKeys);

        if (issueKeys.Count == 0)
        {
            return new IssueTimelineBatchResult([], []);
        }

        var requestedFields = await BuildIssueTimelineRequestedFieldsAsync(cancellationToken)
            .ConfigureAwait(false);
        var issues = new List<IssueTimeline>(issueKeys.Count);
        var failures = new List<LoadFailure>();

        foreach (var issueKeyBatch in BatchIssueKeys(issueKeys, ISSUE_TIMELINE_BULK_FETCH_BATCH_SIZE))
        {
            try
            {
                var issueResponses = await _searchExecutor
                    .GetIssuesAsync(issueKeyBatch, requestedFields, cancellationToken)
                    .ConfigureAwait(false);
                var changelogsByIssueId = await _searchExecutor
                    .GetIssueChangelogsAsync(issueKeyBatch, cancellationToken)
                    .ConfigureAwait(false);
                var returnedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var issueResponse in issueResponses)
                {
                    if (string.IsNullOrWhiteSpace(issueResponse.Key))
                    {
                        continue;
                    }

                    var issueKey = new IssueKey(issueResponse.Key.Trim());
                    _ = returnedKeys.Add(issueKey.Value);

                    try
                    {
                        issues.Add(_mapperFacade.MapIssueTimeline(
                            AttachChangelog(issueResponse, changelogsByIssueId),
                            issueKey));
                    }
                    catch (InvalidOperationException ex)
                    {
                        failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                    }
                }

                foreach (var requestedIssueKey in issueKeyBatch.Where(issueKey => !returnedKeys.Contains(issueKey.Value)))
                {
                    failures.Add(new LoadFailure(
                        requestedIssueKey,
                        new ErrorMessage("Issue was not returned by Jira bulk fetch.")));
                }
            }
            catch (HttpRequestException ex)
            {
                failures.AddRange(BuildBatchFailures(issueKeyBatch, ex));
            }
            catch (InvalidOperationException ex)
            {
                failures.AddRange(BuildBatchFailures(issueKeyBatch, ex));
            }
            catch (JsonException ex)
            {
                failures.AddRange(BuildBatchFailures(issueKeyBatch, ex));
            }
        }

        return new IssueTimelineBatchResult(issues, failures);
    }

    private async Task<IReadOnlyList<string>?> BuildIssueTimelineRequestedFieldsAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_pullRequestFieldName))
        {
            return null;
        }

        var pullRequestFieldId = await ResolvePullRequestFieldIdAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(pullRequestFieldId)
            ? _issueTimelineBaseFields
            : [.. _issueTimelineBaseFields, pullRequestFieldId];
    }

    private async Task<string?> ResolvePullRequestFieldIdAsync(CancellationToken cancellationToken)
    {
        if (_pullRequestFieldIdResolved)
        {
            return _pullRequestFieldId;
        }

        _pullRequestFieldIdResolved = true;
        _pullRequestFieldId = IsCustomFieldId(_pullRequestFieldName)
            ? _pullRequestFieldName
            : await _fieldResolver.TryResolveFieldIdAsync(_pullRequestFieldName, cancellationToken)
                .ConfigureAwait(false);
        return _pullRequestFieldId;
    }

    private static bool IsCustomFieldId(string? fieldName) =>
        !string.IsNullOrWhiteSpace(fieldName)
        && fieldName.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase);

    private static JiraIssueResponse AttachChangelog(
        JiraIssueResponse issueResponse,
        IReadOnlyDictionary<string, IReadOnlyList<JiraHistoryResponse>> changelogsByIssueId)
    {
        var histories = !string.IsNullOrWhiteSpace(issueResponse.Id)
            && changelogsByIssueId.TryGetValue(issueResponse.Id, out var resolvedHistories)
                ? resolvedHistories
                : [];

        return new JiraIssueResponse
        {
            Id = issueResponse.Id,
            Key = issueResponse.Key,
            Fields = issueResponse.Fields,
            Changelog = new JiraChangelogResponse
            {
                Histories = histories
            }
        };
    }

    private static IReadOnlyList<LoadFailure> BuildBatchFailures(
        IReadOnlyList<IssueKey> issueKeys,
        Exception ex) =>
        [.. issueKeys.Select(issueKey => new LoadFailure(issueKey, ErrorMessage.FromException(ex)))];

    private static IEnumerable<IReadOnlyList<IssueKey>> BatchIssueKeys(
        IReadOnlyList<IssueKey> issueKeys,
        int batchSize)
    {
        for (var i = 0; i < issueKeys.Count; i += batchSize)
        {
            var count = Math.Min(batchSize, issueKeys.Count - i);
            var batch = new IssueKey[count];
            for (var j = 0; j < count; j++)
            {
                batch[j] = issueKeys[i + j];
            }

            yield return batch;
        }
    }

    private readonly IJiraSearchExecutor _searchExecutor;
    private readonly IJiraJqlFacade _jqlFacade;
    private readonly string? _pullRequestFieldName;
    private readonly IJiraFieldResolver _fieldResolver;
    private readonly IJiraMapperFacade _mapperFacade;
    private string? _pullRequestFieldId;
    private bool _pullRequestFieldIdResolved;
    private const int ISSUE_TIMELINE_BULK_FETCH_BATCH_SIZE = 100;

    private static readonly string[] _issueTimelineBaseFields =
    [
        "summary",
        "created",
        "resolutiondate",
        "issuetype",
        "status",
        "issuelinks",
        "subtasks"
    ];
}
