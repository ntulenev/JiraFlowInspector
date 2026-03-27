using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents optional report sections and ratio sections.
/// </summary>
public interface IJiraReportSectionsPresenter
{
    void ShowReleaseReportLoadingStarted();

    void ShowGlobalIncidentsReportLoadingStarted();

    void ShowArchTasksReportLoadingStarted();

    void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases);

    void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks);

    void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents);

    void ShowAllTasksRatioLoadingStarted();

    void ShowAllTasksRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        ItemCount createdThisMonth,
        ItemCount openThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames);

    void ShowBugRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

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

    void ShowSpacer();
}
