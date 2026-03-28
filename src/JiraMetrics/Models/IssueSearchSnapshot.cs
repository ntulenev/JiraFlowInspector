using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Shared Jira search results reused across report-context and ratio calculations.
/// </summary>
public sealed record IssueSearchSnapshot(
    IReadOnlyList<IssueListItem> CreatedIssues,
    IReadOnlyList<IssueListItem> DoneIssues,
    IReadOnlyList<IssueListItem> RejectedIssues)
{
    /// <summary>
    /// Builds issue-ratio counters and lists for the snapshot.
    /// </summary>
    /// <returns>Issue ratio snapshot.</returns>
    public IssueRatioSnapshot BuildRatioSnapshot()
    {
        var doneKeys = DoneIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rejectedKeys = RejectedIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var finishedKeys = doneKeys
            .Union(rejectedKeys, StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var openIssues = (IReadOnlyList<IssueListItem>)[.. CreatedIssues
            .Where(issue => !finishedKeys.Contains(issue.Key.Value))
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

        return new IssueRatioSnapshot(
            new ItemCount(CreatedIssues.Count),
            new ItemCount(openIssues.Count),
            new ItemCount(DoneIssues.Count),
            new ItemCount(RejectedIssues.Count),
            new ItemCount(finishedKeys.Count),
            openIssues,
            DoneIssues,
            RejectedIssues);
    }
}
