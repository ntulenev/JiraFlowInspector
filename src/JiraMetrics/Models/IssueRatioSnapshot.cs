using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Aggregated issue ratio counters and issue lists for a specific issue-type filter.
/// </summary>
public sealed record IssueRatioSnapshot(
    ItemCount CreatedThisMonth,
    ItemCount OpenThisMonth,
    ItemCount MovedToDoneThisMonth,
    ItemCount RejectedThisMonth,
    ItemCount FinishedThisMonth,
    IReadOnlyList<IssueListItem> OpenIssues,
    IReadOnlyList<IssueListItem> DoneIssues,
    IReadOnlyList<IssueListItem> RejectedIssues)
{
    /// <summary>
    /// Gets unique issues across open, done, and rejected lists.
    /// </summary>
    public IReadOnlyList<IssueListItem> AllIssues =>
        [.. OpenIssues
            .Concat(DoneIssues)
            .Concat(RejectedIssues)
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Gets unique issues marked as reproduced on production.
    /// </summary>
    public IReadOnlyList<IssueListItem> ReporducedOnProdIssues =>
        [.. AllIssues
            .Where(static issue => issue.ReporducedOnProd)
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
}
