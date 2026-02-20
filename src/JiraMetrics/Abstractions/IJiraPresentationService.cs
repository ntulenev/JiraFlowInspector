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
    /// Shows issue timeline loading start message.
    /// </summary>
    /// <param name="totalIssues">Total issues to load.</param>
    void ShowIssueLoadingStarted(ItemCount totalIssues);

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
    /// Shows issue timeline loading completion message.
    /// </summary>
    /// <param name="loadedIssues">Successfully loaded issues.</param>
    /// <param name="failedIssues">Failed issue loads.</param>
    void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues);

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
    /// Shows bug ratio loading start message.
    /// </summary>
    /// <param name="bugIssueNames">Issue types treated as bug-like issues.</param>
    void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames);

    /// <summary>
    /// Shows bug ratio loading complete message.
    /// </summary>
    /// <param name="createdThisMonth">Count of created bug-like issues in month.</param>
    /// <param name="movedToDoneThisMonth">Count of moved-to-done bug-like issues in month.</param>
    /// <param name="rejectedThisMonth">Count of moved-to-rejected bug-like issues in month.</param>
    /// <param name="finishedThisMonth">Count of finished bug-like issues in month (done + rejected).</param>
    void ShowBugRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    /// <summary>
    /// Shows bug ratio section.
    /// </summary>
    /// <param name="bugIssueNames">Issue types treated as bug-like issues.</param>
    /// <param name="createdThisMonth">Count of created issues in month.</param>
    /// <param name="movedToDoneThisMonth">Count of issues moved to done in month.</param>
    /// <param name="rejectedThisMonth">Count of issues moved to rejected in month.</param>
    /// <param name="finishedThisMonth">Count of finished issues in month (done + rejected).</param>
    /// <param name="openIssues">Issues created this month and not moved to done/rejected this month.</param>
    /// <param name="doneIssues">Issues moved to done this month.</param>
    /// <param name="rejectedIssues">Issues moved to rejected this month.</param>
    void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth,
        IReadOnlyList<IssueListItem> openIssues,
        IReadOnlyList<IssueListItem> doneIssues,
        IReadOnlyList<IssueListItem> rejectedIssues);

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
