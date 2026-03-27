using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Builds report analysis results from loaded issue timelines.
/// </summary>
public interface IJiraApplicationAnalysisFacade
{
    /// <summary>
    /// Analyzes loaded issues using configured issue filters and path requirements.
    /// </summary>
    JiraIssueAnalysisResult Analyze(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<IssueTimeline> rejectIssues,
        IReadOnlyList<LoadFailure> failures,
        AppSettings settings);
}

