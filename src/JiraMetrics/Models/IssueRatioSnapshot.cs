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
    IReadOnlyList<IssueListItem> RejectedIssues);
