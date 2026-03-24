namespace JiraMetrics.Models;

/// <summary>
/// Describes the result of applying issue analysis filters.
/// </summary>
public enum JiraIssueAnalysisOutcome
{
    /// <summary>
    /// Analysis completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// No loaded issues matched the configured issue-type filter.
    /// </summary>
    NoIssuesMatchedTypeFilter = 1,

    /// <summary>
    /// No filtered issues matched the configured required stage filter.
    /// </summary>
    NoIssuesMatchedRequiredStage = 2
}
