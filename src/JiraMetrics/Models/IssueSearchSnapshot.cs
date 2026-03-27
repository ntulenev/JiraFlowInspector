namespace JiraMetrics.Models;

/// <summary>
/// Shared Jira search results reused across report-context and ratio calculations.
/// </summary>
public sealed record IssueSearchSnapshot(
    IReadOnlyList<IssueListItem> CreatedIssues,
    IReadOnlyList<IssueListItem> DoneIssues,
    IReadOnlyList<IssueListItem> RejectedIssues);
