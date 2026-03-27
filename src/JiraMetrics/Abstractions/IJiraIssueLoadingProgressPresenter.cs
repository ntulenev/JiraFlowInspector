using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents issue timeline loading progress.
/// </summary>
public interface IJiraIssueLoadingProgressPresenter
{
    void ShowIssueLoadingStarted(ItemCount totalIssues);

    void ShowIssueLoaded(IssueKey issueKey);

    void ShowIssueFailed(IssueKey issueKey);

    void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues);

    void ShowSpacer();
}
