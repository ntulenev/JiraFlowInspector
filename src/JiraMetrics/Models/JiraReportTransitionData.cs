using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Contains transition analysis results used by report sections.
/// </summary>
public sealed class JiraReportTransitionData
{
    /// <summary>
    /// Gets issues moved to done in the selected period.
    /// </summary>
    public IReadOnlyList<IssueTimeline> DoneIssues { get; init; } = [];

    /// <summary>
    /// Gets P75 work-duration summaries per issue type for done issues.
    /// </summary>
    public IReadOnlyList<IssueTypeWorkDays75Summary> DoneDaysAtWork75PerType { get; init; } = [];

    /// <summary>
    /// Gets issues matched by the configured custom transition analysis.
    /// </summary>
    public IReadOnlyList<CustomTransitionIssue> CustomTransitionIssues { get; init; } = [];

    /// <summary>
    /// Gets custom transition P75 duration summaries per issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeDuration75Summary> CustomTransitionDuration75PerType { get; init; } = [];

    /// <summary>
    /// Gets QA-specific transition measurements.
    /// </summary>
    public QaTransitionAnalysis QaTransitionAnalysis { get; init; } = QaTransitionAnalysis.Empty;

    /// <summary>
    /// Gets issues moved to rejected in the selected period.
    /// </summary>
    public IReadOnlyList<IssueTimeline> RejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets the transition path summary.
    /// </summary>
    public PathGroupsSummary PathSummary { get; init; } = new(
        new ItemCount(0),
        new ItemCount(0),
        new ItemCount(0),
        new ItemCount(0));

    /// <summary>
    /// Gets transition path groups.
    /// </summary>
    public IReadOnlyList<PathGroup> PathGroups { get; init; } = [];
}
