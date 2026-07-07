using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads reusable issue search datasets for ratio and report-context calculations.
/// </summary>
internal sealed class IssueSearchSnapshotLoader
{

    public IssueSearchSnapshotLoader(IJiraIssueSearchClient issueSearchClient)
    {
        ArgumentNullException.ThrowIfNull(issueSearchClient);
        _issueSearchClient = issueSearchClient;
    }

    public async Task<IssueSearchSnapshot> LoadAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(issueTypes);

        var createdIssues = await _issueSearchClient.GetIssuesCreatedThisMonthAsync(
            settings.ProjectKey,
            issueTypes,
            cancellationToken,
            CreateReporducedOnProdFieldName(settings, issueTypes)).ConfigureAwait(false);
        var doneIssues = await _issueSearchClient.GetIssuesMovedToDoneThisMonthAsync(
            settings.ProjectKey,
            settings.DoneStatusName,
            issueTypes,
            cancellationToken,
            CreateReporducedOnProdFieldName(settings, issueTypes)).ConfigureAwait(false);

        IReadOnlyList<IssueListItem> rejectedIssues = [];
        if (settings.RejectStatusName is { } rejectStatusName)
        {
            rejectedIssues = await _issueSearchClient.GetIssuesMovedToDoneThisMonthAsync(
                settings.ProjectKey,
                rejectStatusName,
                issueTypes,
                cancellationToken,
                CreateReporducedOnProdFieldName(settings, issueTypes)).ConfigureAwait(false);
        }

        return new IssueSearchSnapshot(createdIssues, doneIssues, rejectedIssues);
    }

    private static JiraFieldName? CreateReporducedOnProdFieldName(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes)
    {
        if (string.IsNullOrWhiteSpace(settings.BugReporducedOnProdFieldName)
            || issueTypes.Count == 0
            || !issueTypes.SequenceEqual(settings.BugIssueNames))
        {
            return null;
        }

        return new JiraFieldName(settings.BugReporducedOnProdFieldName);
    }
    private readonly IJiraIssueSearchClient _issueSearchClient;
}

