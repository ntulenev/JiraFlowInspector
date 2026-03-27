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
    /// <param name="snapshot">All-tasks ratio snapshot.</param>
    void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot);

    /// <summary>
    /// Shows all-tasks ratio section.
    /// </summary>
    /// <param name="customFieldName">Optional custom field filter name.</param>
    /// <param name="customFieldValue">Optional custom field filter value.</param>
    /// <param name="snapshot">All-tasks ratio snapshot.</param>
    void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot);

    /// <summary>
    /// Shows bug ratio loading start message.
    /// </summary>
    /// <param name="bugIssueNames">Configured bug-like issue types.</param>
    void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames);

    /// <summary>
    /// Shows bug ratio loading completion message.
    /// </summary>
    /// <param name="snapshot">Bug-ratio snapshot.</param>
    void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot);

    /// <summary>
    /// Shows bug ratio section and issue lists.
    /// </summary>
    /// <param name="bugIssueNames">Configured bug-like issue types.</param>
    /// <param name="customFieldName">Optional custom field filter name.</param>
    /// <param name="customFieldValue">Optional custom field filter value.</param>
    /// <param name="snapshot">Bug-ratio snapshot.</param>
    void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();
}

