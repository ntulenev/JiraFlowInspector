using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads created/open/finished issue ratios for a configured issue-type filter.
/// </summary>
internal sealed class JiraIssueRatioLoader
{
    private readonly IJiraApiClient _apiClient;

    public JiraIssueRatioLoader(IJiraApiClient apiClient)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        _apiClient = apiClient;
    }

    public async Task<IssueRatioSnapshot> LoadAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(issueTypes);

        var createdIssues = await _apiClient.GetIssuesCreatedThisMonthAsync(
            settings.ProjectKey,
            issueTypes,
            cancellationToken).ConfigureAwait(false);
        var doneIssues = await _apiClient.GetIssuesMovedToDoneThisMonthAsync(
            settings.ProjectKey,
            settings.DoneStatusName,
            issueTypes,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<IssueListItem> rejectedIssues = [];
        if (settings.RejectStatusName is { } rejectStatusName)
        {
            rejectedIssues = await _apiClient.GetIssuesMovedToDoneThisMonthAsync(
                settings.ProjectKey,
                rejectStatusName,
                issueTypes,
                cancellationToken).ConfigureAwait(false);
        }

        var doneKeys = doneIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rejectedKeys = rejectedIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var finishedKeys = doneKeys
            .Union(rejectedKeys, StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var openIssues = (IReadOnlyList<IssueListItem>)[.. createdIssues
            .Where(issue => !finishedKeys.Contains(issue.Key.Value))
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

        return new IssueRatioSnapshot(
            new ItemCount(createdIssues.Count),
            new ItemCount(openIssues.Count),
            new ItemCount(doneIssues.Count),
            new ItemCount(rejectedIssues.Count),
            new ItemCount(finishedKeys.Count),
            openIssues,
            doneIssues,
            rejectedIssues);
    }
}
