namespace JiraMetrics.Models;

/// <summary>
/// Aggregated issue analysis results used by presentation and PDF rendering.
/// </summary>
public sealed record JiraIssueAnalysisResult
{
    /// <summary>
    /// Gets the overall analysis outcome.
    /// </summary>
    public required JiraIssueAnalysisOutcome Outcome { get; init; }

    /// <summary>
    /// Gets filtered done issues.
    /// </summary>
    public IReadOnlyList<IssueTimeline> DoneIssues { get; init; } = [];

    /// <summary>
    /// Gets filtered rejected issues.
    /// </summary>
    public IReadOnlyList<IssueTimeline> RejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets P75 work-duration summaries per issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeWorkDays75Summary> DoneDaysAtWork75PerType { get; init; } = [];

    /// <summary>
    /// Gets grouped issue paths.
    /// </summary>
    public IReadOnlyList<PathGroup> PathGroups { get; init; } = [];

    /// <summary>
    /// Gets path-group summary for the analyzed issues.
    /// </summary>
    public PathGroupsSummary? PathSummary { get; init; }

    /// <summary>
    /// Creates a result for a successful analysis.
    /// </summary>
    public static JiraIssueAnalysisResult Success(
        IReadOnlyList<IssueTimeline> doneIssues,
        IReadOnlyList<IssueTimeline> rejectedIssues,
        IReadOnlyList<IssueTypeWorkDays75Summary> doneDaysAtWork75PerType,
        IReadOnlyList<PathGroup> pathGroups,
        PathGroupsSummary pathSummary)
    {
        ArgumentNullException.ThrowIfNull(doneIssues);
        ArgumentNullException.ThrowIfNull(rejectedIssues);
        ArgumentNullException.ThrowIfNull(doneDaysAtWork75PerType);
        ArgumentNullException.ThrowIfNull(pathGroups);
        ArgumentNullException.ThrowIfNull(pathSummary);

        return new JiraIssueAnalysisResult
        {
            Outcome = JiraIssueAnalysisOutcome.Success,
            DoneIssues = doneIssues,
            RejectedIssues = rejectedIssues,
            DoneDaysAtWork75PerType = doneDaysAtWork75PerType,
            PathGroups = pathGroups,
            PathSummary = pathSummary
        };
    }

    /// <summary>
    /// Creates a result when no issues match the configured issue-type filter.
    /// </summary>
    public static JiraIssueAnalysisResult NoIssuesMatchedTypeFilter() =>
        new()
        {
            Outcome = JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter
        };

    /// <summary>
    /// Creates a result when no issues match the required stage filter.
    /// </summary>
    public static JiraIssueAnalysisResult NoIssuesMatchedRequiredStage() =>
        new()
        {
            Outcome = JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage
        };
}
