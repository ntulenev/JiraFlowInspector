using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Provides UI presentation operations.
/// </summary>
public interface IJiraPresentationService
{
    /// <summary>
    /// Shows auth start message.
    /// </summary>
    void ShowAuthenticationStarted();

    /// <summary>
    /// Shows successful authentication message.
    /// </summary>
    /// <param name="user">Authenticated user.</param>
    void ShowAuthenticationSucceeded(JiraAuthUser user);

    /// <summary>
    /// Shows authentication failure message.
    /// </summary>
    /// <param name="errorMessage">Error message.</param>
    void ShowAuthenticationFailed(ErrorMessage errorMessage);

    /// <summary>
    /// Shows issue search failure message.
    /// </summary>
    /// <param name="errorMessage">Error message.</param>
    void ShowIssueSearchFailed(ErrorMessage errorMessage);

    /// <summary>
    /// Shows report header.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="issueCount">Issue count.</param>
    void ShowReportHeader(AppSettings settings, ItemCount issueCount);

    /// <summary>
    /// Shows no-issue message for search filter.
    /// </summary>
    void ShowNoIssuesMatchedFilter();

    /// <summary>
    /// Shows successful issue load line.
    /// </summary>
    /// <param name="issueKey">Issue key.</param>
    void ShowIssueLoaded(IssueKey issueKey);

    /// <summary>
    /// Shows failed issue load line.
    /// </summary>
    /// <param name="issueKey">Issue key.</param>
    void ShowIssueFailed(IssueKey issueKey);

    /// <summary>
    /// Shows spacer line.
    /// </summary>
    void ShowSpacer();

    /// <summary>
    /// Shows no-loaded-issues message.
    /// </summary>
    void ShowNoIssuesLoaded();

    /// <summary>
    /// Shows no-stage-match message.
    /// </summary>
    void ShowNoIssuesMatchedRequiredStage();

    /// <summary>
    /// Shows table with issues moved to done.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <param name="doneStatusName">Done status name.</param>
    void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName);

    /// <summary>
    /// Shows path group summary counters.
    /// </summary>
    /// <param name="summary">Summary counters.</param>
    void ShowPathGroupsSummary(PathGroupsSummary summary);

    /// <summary>
    /// Shows path group details.
    /// </summary>
    /// <param name="groups">Path groups.</param>
    void ShowPathGroups(IReadOnlyList<PathGroup> groups);

    /// <summary>
    /// Shows failed issue table.
    /// </summary>
    /// <param name="failures">Failures.</param>
    void ShowFailures(IReadOnlyList<LoadFailure> failures);
}
