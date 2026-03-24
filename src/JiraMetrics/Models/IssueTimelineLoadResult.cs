using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Result of loading issue timelines for done and rejected issues.
/// </summary>
public sealed record IssueTimelineLoadResult(
    IReadOnlyList<IssueTimeline> DoneIssues,
    IReadOnlyList<IssueTimeline> RejectIssues,
    IReadOnlyList<LoadFailure> Failures,
    ItemCount LoadedIssueCount);
