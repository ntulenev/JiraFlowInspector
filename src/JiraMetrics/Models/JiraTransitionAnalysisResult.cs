namespace JiraMetrics.Models;

/// <summary>
/// Represents one valid outcome of the transition-analysis workflow.
/// </summary>
internal abstract record JiraTransitionAnalysisResult
{
    private JiraTransitionAnalysisResult()
    {
    }

    internal sealed record NoTransitionIssues : JiraTransitionAnalysisResult;

    internal sealed record NoIssuesLoaded(IssueTimelineLoadResult LoadResult) :
        JiraTransitionAnalysisResult;

    internal sealed record NoIssuesMatchedTypeFilter(IssueTimelineLoadResult LoadResult) :
        JiraTransitionAnalysisResult;

    internal sealed record NoIssuesMatchedRequiredStage(IssueTimelineLoadResult LoadResult) :
        JiraTransitionAnalysisResult;

    internal sealed record Success(
        IssueTimelineLoadResult LoadResult,
        SuccessfulJiraIssueAnalysis Analysis) : JiraTransitionAnalysisResult;
}
