using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads detailed issue timelines and reports progress through the presentation service.
/// </summary>
internal sealed class JiraIssueTimelineLoader
{

    public JiraIssueTimelineLoader(
        IJiraIssueTimelineClient issueTimelineClient,
        IJiraIssueLoadingProgressPresenter progressPresenter)
    {
        ArgumentNullException.ThrowIfNull(issueTimelineClient);
        ArgumentNullException.ThrowIfNull(progressPresenter);
        _issueTimelineClient = issueTimelineClient;
        _progressPresenter = progressPresenter;
    }

    public async Task<IssueTimelineLoadResult> LoadAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueKeys);
        ArgumentNullException.ThrowIfNull(rejectIssueKeys);

        var uniqueIssueKeys = BuildUniqueIssueKeys(issueKeys, rejectIssueKeys);
        _progressPresenter.ShowIssueLoadingStarted(new ItemCount(uniqueIssueKeys.Count));

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

        _progressPresenter.ShowIssueLoadingCompleted(
            new ItemCount(loadedIssuesByKey.Count),
            new ItemCount(failures.Count));
        _progressPresenter.ShowSpacer();

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

        var batchResult = await _issueTimelineClient
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
                _progressPresenter.ShowIssueLoaded(issueKey);
                outcomes.Add(new IssueLoadOutcome(issueKey, issue, null));
            }
            else if (failuresByKey.TryGetValue(issueKey.Value, out var failure))
            {
                _progressPresenter.ShowIssueFailed(issueKey);
                outcomes.Add(new IssueLoadOutcome(issueKey, null, failure));
            }
            else
            {
                var missingIssueFailure = new LoadFailure(
                    issueKey,
                    new ErrorMessage("Issue timeline was not returned by Jira."));
                _progressPresenter.ShowIssueFailed(issueKey);
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
    private readonly IJiraIssueTimelineClient _issueTimelineClient;
    private readonly IJiraIssueLoadingProgressPresenter _progressPresenter;
}

