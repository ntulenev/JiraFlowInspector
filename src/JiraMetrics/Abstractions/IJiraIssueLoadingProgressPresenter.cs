using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents issue timeline loading progress.
/// </summary>
public interface IJiraIssueLoadingProgressPresenter
{
    /// <summary>
    /// Shows issue loading start message.
    /// </summary>
    /// <param name="totalIssues">Total issues to load.</param>
    void ShowIssueLoadingStarted(ItemCount totalIssues);

    /// <summary>
    /// Shows successful issue load progress.
    /// </summary>
    /// <param name="issueKey">Loaded issue key.</param>
    void ShowIssueLoaded(IssueKey issueKey);

    /// <summary>
    /// Shows failed issue load progress.
    /// </summary>
    /// <param name="issueKey">Failed issue key.</param>
    void ShowIssueFailed(IssueKey issueKey);

    /// <summary>
    /// Shows issue loading completion message.
    /// </summary>
    /// <param name="loadedIssues">Successfully loaded issues.</param>
    /// <param name="failedIssues">Failed issue loads.</param>
    void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();
}
