namespace JiraMetrics.Models;

/// <summary>
/// Batched issue timeline load result.
/// </summary>
public sealed record IssueTimelineBatchResult(
    IReadOnlyList<IssueTimeline> Issues,
    IReadOnlyList<LoadFailure> Failures);
