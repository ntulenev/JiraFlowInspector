namespace JiraMetrics.Models;

/// <summary>
/// Batched issue timeline load result.
/// </summary>
/// <param name="Issues">The <paramref name="Issues"/> value.</param>
/// <param name="Failures">The <paramref name="Failures"/> value.</param>
public sealed record IssueTimelineBatchResult(
    IReadOnlyList<IssueTimeline> Issues,
    IReadOnlyList<LoadFailure> Failures);
