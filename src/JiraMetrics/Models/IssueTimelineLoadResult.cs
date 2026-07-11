using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Result of loading issue timelines for done and rejected issues.
/// </summary>
/// <param name="DoneIssues">The <paramref name="DoneIssues"/> value.</param>
/// <param name="RejectIssues">The <paramref name="RejectIssues"/> value.</param>
/// <param name="Failures">The <paramref name="Failures"/> value.</param>
/// <param name="LoadedIssueCount">The <paramref name="LoadedIssueCount"/> value.</param>
public sealed record IssueTimelineLoadResult(
    IReadOnlyList<IssueTimeline> DoneIssues,
    IReadOnlyList<IssueTimeline> RejectIssues,
    IReadOnlyList<LoadFailure> Failures,
    ItemCount LoadedIssueCount);
