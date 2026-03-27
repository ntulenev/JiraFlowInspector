using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Presentation;

/// <summary>
/// Presents optional report sections and ratio sections.
/// </summary>
public interface IJiraReportSectionsPresenter
{
    /// <summary>
    /// Shows release report loading start message.
    /// </summary>
    void ShowReleaseReportLoadingStarted();

    /// <summary>
    /// Shows global incidents report loading start message.
    /// </summary>
    void ShowGlobalIncidentsReportLoadingStarted();

    /// <summary>
    /// Shows architecture tasks report loading start message.
    /// </summary>
    void ShowArchTasksReportLoadingStarted();

    /// <summary>
    /// Shows release report section.
    /// </summary>
    /// <param name="settings">Release report settings.</param>
    /// <param name="reportPeriod">Report period.</param>
    /// <param name="releases">Release issues.</param>
    void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases);

    /// <summary>
    /// Shows architecture tasks report section.
    /// </summary>
    /// <param name="settings">Architecture tasks settings.</param>
    /// <param name="tasks">Architecture tasks.</param>
    void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks);

    /// <summary>
    /// Shows global incidents report section.
    /// </summary>
    /// <param name="settings">Global incidents settings.</param>
    /// <param name="reportPeriod">Report period.</param>
    /// <param name="incidents">Incident rows.</param>
    void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents);

    /// <summary>
    /// Shows all-tasks ratio loading start message.
    /// </summary>
    void ShowAllTasksRatioLoadingStarted();

    /// <summary>
    /// Shows all-tasks ratio loading completion message.
    /// </summary>
    /// <param name="createdThisMonth">Created issues count.</param>
    /// <param name="movedToDoneThisMonth">Done issues count.</param>
    /// <param name="rejectedThisMonth">Rejected issues count.</param>
    /// <param name="finishedThisMonth">Finished issues count.</param>
    void ShowAllTasksRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    /// <summary>
    /// Shows all-tasks ratio section.
    /// </summary>
    /// <param name="customFieldName">Optional custom field filter name.</param>
    /// <param name="customFieldValue">Optional custom field filter value.</param>
    /// <param name="createdThisMonth">Created issues count.</param>
    /// <param name="openThisMonth">Open issues count.</param>
    /// <param name="movedToDoneThisMonth">Done issues count.</param>
    /// <param name="rejectedThisMonth">Rejected issues count.</param>
    /// <param name="finishedThisMonth">Finished issues count.</param>
    void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        ItemCount createdThisMonth,
        ItemCount openThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    /// <summary>
    /// Shows bug ratio loading start message.
    /// </summary>
    /// <param name="bugIssueNames">Configured bug-like issue types.</param>
    void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames);

    /// <summary>
    /// Shows bug ratio loading completion message.
    /// </summary>
    /// <param name="createdThisMonth">Created issues count.</param>
    /// <param name="movedToDoneThisMonth">Done issues count.</param>
    /// <param name="rejectedThisMonth">Rejected issues count.</param>
    /// <param name="finishedThisMonth">Finished issues count.</param>
    void ShowBugRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    /// <summary>
    /// Shows bug ratio section and issue lists.
    /// </summary>
    /// <param name="bugIssueNames">Configured bug-like issue types.</param>
    /// <param name="customFieldName">Optional custom field filter name.</param>
    /// <param name="customFieldValue">Optional custom field filter value.</param>
    /// <param name="createdThisMonth">Created issues count.</param>
    /// <param name="movedToDoneThisMonth">Done issues count.</param>
    /// <param name="rejectedThisMonth">Rejected issues count.</param>
    /// <param name="finishedThisMonth">Finished issues count.</param>
    /// <param name="openIssues">Open issues in the selected period.</param>
    /// <param name="doneIssues">Done issues in the selected period.</param>
    /// <param name="rejectedIssues">Rejected issues in the selected period.</param>
    void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth,
        IReadOnlyList<IssueListItem> openIssues,
        IReadOnlyList<IssueListItem> doneIssues,
        IReadOnlyList<IssueListItem> rejectedIssues);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();
}

