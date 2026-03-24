using System.Text.Json;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads detailed issue timelines and reports progress through the presentation service.
/// </summary>
internal sealed class JiraIssueTimelineLoader
{
    private readonly IJiraApiClient _apiClient;
    private readonly IJiraPresentationService _presentationService;

    public JiraIssueTimelineLoader(
        IJiraApiClient apiClient,
        IJiraPresentationService presentationService)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(presentationService);
        _apiClient = apiClient;
        _presentationService = presentationService;
    }

    public async Task<IssueTimelineLoadResult> LoadAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueKeys);
        ArgumentNullException.ThrowIfNull(rejectIssueKeys);

        var uniqueIssueKeysToLoad = issueKeys
            .Concat(rejectIssueKeys)
            .Select(static key => key.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        _presentationService.ShowIssueLoadingStarted(new ItemCount(uniqueIssueKeysToLoad));

        var issues = new List<IssueTimeline>();
        var rejectIssues = new List<IssueTimeline>();
        var loadedIssuesByKey = new Dictionary<string, IssueTimeline>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<LoadFailure>();

        foreach (var issueKey in issueKeys)
        {
            var issue = await TryLoadIssueAsync(issueKey, failures, cancellationToken).ConfigureAwait(false);
            if (issue is null)
            {
                continue;
            }

            issues.Add(issue);
            loadedIssuesByKey[issue.Key.Value] = issue;
        }

        foreach (var issueKey in rejectIssueKeys)
        {
            if (loadedIssuesByKey.TryGetValue(issueKey.Value, out var loadedIssue))
            {
                rejectIssues.Add(loadedIssue);
                continue;
            }

            var issue = await TryLoadIssueAsync(issueKey, failures, cancellationToken).ConfigureAwait(false);
            if (issue is null)
            {
                continue;
            }

            rejectIssues.Add(issue);
            loadedIssuesByKey[issue.Key.Value] = issue;
        }

        _presentationService.ShowIssueLoadingCompleted(
            new ItemCount(loadedIssuesByKey.Count),
            new ItemCount(failures.Count));
        _presentationService.ShowSpacer();

        return new IssueTimelineLoadResult(
            issues,
            rejectIssues,
            failures,
            new ItemCount(loadedIssuesByKey.Count));
    }

    private async Task<IssueTimeline?> TryLoadIssueAsync(
        IssueKey issueKey,
        List<LoadFailure> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _apiClient
                .GetIssueTimelineAsync(issueKey, cancellationToken)
                .ConfigureAwait(false);
            _presentationService.ShowIssueLoaded(issueKey);
            return issue;
        }
        catch (HttpRequestException ex)
        {
            failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
        }
        catch (InvalidOperationException ex)
        {
            failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
        }
        catch (JsonException ex)
        {
            failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
        }

        _presentationService.ShowIssueFailed(issueKey);
        return null;
    }
}
