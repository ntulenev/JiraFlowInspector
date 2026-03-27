using System.Text.Json;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

namespace JiraMetrics.API;

internal sealed class JiraIssueTimelineClient : IJiraIssueTimelineClient
{
    private readonly IJiraSearchExecutor _searchExecutor;
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

    public JiraIssueTimelineClient(
        IJiraSearchExecutor searchExecutor,
        IOptions<AppSettings> settings,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapperFacade)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(fieldResolver);
        ArgumentNullException.ThrowIfNull(mapperFacade);

        _searchExecutor = searchExecutor;
        _pullRequestFieldName = settings.Value.PullRequestFieldName;
        _fieldResolver = fieldResolver;
        _mapperFacade = mapperFacade;
    }

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
}
