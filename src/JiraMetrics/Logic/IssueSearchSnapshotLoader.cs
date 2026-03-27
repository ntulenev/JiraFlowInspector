using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads reusable issue search datasets for ratio and report-context calculations.
/// </summary>
internal sealed class IssueSearchSnapshotLoader
{
    private readonly IJiraApiClient _apiClient;

    public IssueSearchSnapshotLoader(IJiraApiClient apiClient)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        _apiClient = apiClient;
    }

    public async Task<IssueSearchSnapshot> LoadAsync(
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

        return new IssueSearchSnapshot(createdIssues, doneIssues, rejectedIssues);
    }
}
