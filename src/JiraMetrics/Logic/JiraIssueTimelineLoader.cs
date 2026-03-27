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

        var uniqueIssueKeys = BuildUniqueIssueKeys(issueKeys, rejectIssueKeys);
        _presentationService.ShowIssueLoadingStarted(new ItemCount(uniqueIssueKeys.Count));

        var loadOutcomes = await LoadIssuesAsync(uniqueIssueKeys, cancellationToken).ConfigureAwait(false);
        var loadedIssuesByKey = loadOutcomes
            .Where(static outcome => outcome.Issue is not null)
            .ToDictionary(
                static outcome => outcome.Key.Value,
                static outcome => outcome.Issue!,
                StringComparer.OrdinalIgnoreCase);
        var failures = loadOutcomes
            .Where(static outcome => outcome.Failure is not null)
            .Select(static outcome => outcome.Failure!)
            .ToList();
        var issues = BuildLoadedIssueList(issueKeys, loadedIssuesByKey);
        var rejectIssues = BuildLoadedIssueList(rejectIssueKeys, loadedIssuesByKey);

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

    private async Task<IReadOnlyList<IssueLoadOutcome>> LoadIssuesAsync(
        List<IssueKey> issueKeys,
        CancellationToken cancellationToken)
    {
        if (issueKeys.Count == 0)
        {
            return [];
        }

        var batchResult = await _apiClient
            .GetIssueTimelinesAsync(issueKeys, cancellationToken)
            .ConfigureAwait(false);
        var loadedIssuesByKey = batchResult.Issues.ToDictionary(
            static issue => issue.Key.Value,
            StringComparer.OrdinalIgnoreCase);
        var failuresByKey = batchResult.Failures.ToDictionary(
            static failure => failure.IssueKey.Value,
            StringComparer.OrdinalIgnoreCase);
        var outcomes = new List<IssueLoadOutcome>(issueKeys.Count);

        foreach (var issueKey in issueKeys)
        {
            if (loadedIssuesByKey.TryGetValue(issueKey.Value, out var issue))
            {
                _presentationService.ShowIssueLoaded(issueKey);
                outcomes.Add(new IssueLoadOutcome(issueKey, issue, null));
            }
            else if (failuresByKey.TryGetValue(issueKey.Value, out var failure))
            {
                _presentationService.ShowIssueFailed(issueKey);
                outcomes.Add(new IssueLoadOutcome(issueKey, null, failure));
            }
            else
            {
                var missingIssueFailure = new LoadFailure(
                    issueKey,
                    new ErrorMessage("Issue timeline was not returned by Jira."));
                _presentationService.ShowIssueFailed(issueKey);
                outcomes.Add(new IssueLoadOutcome(issueKey, null, missingIssueFailure));
            }
        }

        return outcomes;
    }

    private static List<IssueKey> BuildUniqueIssueKeys(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys)
    {
        var uniqueIssueKeys = new List<IssueKey>(issueKeys.Count + rejectIssueKeys.Count);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var issueKey in issueKeys.Concat(rejectIssueKeys))
        {
            if (seenKeys.Add(issueKey.Value))
            {
                uniqueIssueKeys.Add(issueKey);
            }
        }

        return uniqueIssueKeys;
    }

    private static List<IssueTimeline> BuildLoadedIssueList(
        IReadOnlyList<IssueKey> issueKeys,
        Dictionary<string, IssueTimeline> loadedIssuesByKey)
    {
        var issues = new List<IssueTimeline>(issueKeys.Count);

        foreach (var issueKey in issueKeys)
        {
            if (loadedIssuesByKey.TryGetValue(issueKey.Value, out var issue))
            {
                issues.Add(issue);
            }
        }

        return issues;
    }

    private sealed record IssueLoadOutcome(
        IssueKey Key,
        IssueTimeline? Issue,
        LoadFailure? Failure);
}
