namespace JiraMetrics.Models;

/// <summary>
/// Represents one valid outcome of applying issue-analysis filters.
/// </summary>
public abstract record JiraIssueAnalysisResult
{
    private protected JiraIssueAnalysisResult()
    {
    }
}

/// <summary>
/// Represents a completed analysis with all report datasets.
/// </summary>
public sealed record SuccessfulJiraIssueAnalysis : JiraIssueAnalysisResult
{
    /// <summary>
    /// Initializes a successful analysis result.
    /// </summary>
    public SuccessfulJiraIssueAnalysis(
        IReadOnlyList<IssueTimeline> doneIssues,
        IReadOnlyList<IssueTimeline> rejectedIssues,
        IReadOnlyList<IssueTypeWorkDays75Summary> doneDaysAtWork75PerType,
        IReadOnlyList<CustomTransitionIssue> customTransitionIssues,
        IReadOnlyList<IssueTypeDuration75Summary> customTransitionDuration75PerType,
        IReadOnlyList<PathGroup> pathGroups,
        PathGroupsSummary pathSummary,
        QaTransitionAnalysis? qaTransitionAnalysis = null)
    {
        ArgumentNullException.ThrowIfNull(doneIssues);
        ArgumentNullException.ThrowIfNull(rejectedIssues);
        ArgumentNullException.ThrowIfNull(doneDaysAtWork75PerType);
        ArgumentNullException.ThrowIfNull(customTransitionIssues);
        ArgumentNullException.ThrowIfNull(customTransitionDuration75PerType);
        ArgumentNullException.ThrowIfNull(pathGroups);
        ArgumentNullException.ThrowIfNull(pathSummary);

        DoneIssues = doneIssues;
        RejectedIssues = rejectedIssues;
        DoneDaysAtWork75PerType = doneDaysAtWork75PerType;
        CustomTransitionIssues = customTransitionIssues;
        CustomTransitionDuration75PerType = customTransitionDuration75PerType;
        QaTransitionAnalysis = qaTransitionAnalysis ?? QaTransitionAnalysis.Empty;
        PathGroups = pathGroups;
        PathSummary = pathSummary;
    }

    /// <summary>
    /// Gets filtered done issues.
    /// </summary>
    public IReadOnlyList<IssueTimeline> DoneIssues { get; }

    /// <summary>
    /// Gets filtered rejected issues.
    /// </summary>
    public IReadOnlyList<IssueTimeline> RejectedIssues { get; }

    /// <summary>
    /// Gets P75 work-duration summaries per issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeWorkDays75Summary> DoneDaysAtWork75PerType { get; }

    /// <summary>
    /// Gets issues matched by configured custom transition analysis.
    /// </summary>
    public IReadOnlyList<CustomTransitionIssue> CustomTransitionIssues { get; }

    /// <summary>
    /// Gets custom transition P75 duration summaries per issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeDuration75Summary> CustomTransitionDuration75PerType { get; }

    /// <summary>
    /// Gets QA-specific transition measurements.
    /// </summary>
    public QaTransitionAnalysis QaTransitionAnalysis { get; }

    /// <summary>
    /// Gets grouped issue paths.
    /// </summary>
    public IReadOnlyList<PathGroup> PathGroups { get; }

    /// <summary>
    /// Gets path-group summary for the analyzed issues.
    /// </summary>
    public PathGroupsSummary PathSummary { get; }
}

/// <summary>
/// Represents an analysis where no issue matched the issue-type filter.
/// </summary>
public sealed record NoIssuesMatchedTypeFilterAnalysis : JiraIssueAnalysisResult;

/// <summary>
/// Represents an analysis where no issue matched the required-stage filter.
/// </summary>
public sealed record NoIssuesMatchedRequiredStageAnalysis : JiraIssueAnalysisResult;
