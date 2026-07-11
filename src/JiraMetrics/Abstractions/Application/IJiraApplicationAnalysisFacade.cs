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
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="rejectIssues">The <paramref name="rejectIssues"/> value.</param>
    /// <param name="failures">The <paramref name="failures"/> value.</param>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    JiraIssueAnalysisResult Analyze(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<IssueTimeline> rejectIssues,
        IReadOnlyList<LoadFailure> failures,
        AppSettings settings);
}

