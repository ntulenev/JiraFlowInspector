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
    /// Shows top-level period context and optional created-after.
    /// </summary>
    /// <param name="reportPeriod">Selected report period.</param>
    /// <param name="createdAfter">Optional created-after date.</param>
    void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter);

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
    /// Shows short progress message for heavy processing steps after loading.
    /// </summary>
    /// <param name="message">Step description.</param>
    void ShowProcessingStep(string message);

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
    /// Shows days-at-work P75 grouped by issue type for done issues.
    /// </summary>
    /// <param name="summaries">P75 summaries per issue type.</param>
    /// <param name="doneStatusName">Done status name used as completion point.</param>
    void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName);

    /// <summary>
    /// Shows table with issues moved to reject.
    /// </summary>
    /// <param name="issues">Issues.</param>
    /// <param name="rejectStatusName">Reject status name.</param>
    void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName);

    /// <summary>
    /// Shows path group summary counters.
    /// </summary>
    /// <param name="summary">Summary counters.</param>
    void ShowPathGroupsSummary(PathGroupsSummary summary);

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
    /// <param name="reportPeriod">Selected report period.</param>
    /// <param name="releases">Release issues in period.</param>
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
    /// <param name="settings">Global incidents report settings.</param>
    /// <param name="reportPeriod">Selected report period.</param>
    /// <param name="incidents">Incident issues in period.</param>
    void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents);

    /// <summary>
    /// Shows all-tasks ratio loading start message.
    /// </summary>
    void ShowAllTasksRatioLoadingStarted();

    /// <summary>
    /// Shows all-tasks ratio loading complete message.
    /// </summary>
    /// <param name="createdThisMonth">Count of created issues in month.</param>
    /// <param name="movedToDoneThisMonth">Count of moved-to-done issues in month.</param>
    /// <param name="rejectedThisMonth">Count of moved-to-rejected issues in month.</param>
    /// <param name="finishedThisMonth">Count of finished issues in month (done + rejected).</param>
    void ShowAllTasksRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth);

    /// <summary>
    /// Shows all-tasks ratio section.
    /// </summary>
    /// <param name="customFieldName">Optional custom field name used for filtering.</param>
    /// <param name="customFieldValue">Optional custom field value used for filtering.</param>
    /// <param name="createdThisMonth">Count of created issues in month.</param>
    /// <param name="openThisMonth">Count of open issues created in month and not finished within month.</param>
    /// <param name="movedToDoneThisMonth">Count of issues moved to done in month.</param>
    /// <param name="rejectedThisMonth">Count of issues moved to rejected in month.</param>
    /// <param name="finishedThisMonth">Count of finished issues in month (done + rejected).</param>
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
    /// <param name="customFieldName">Optional custom field name used for filtering.</param>
    /// <param name="customFieldValue">Optional custom field value used for filtering.</param>
    /// <param name="createdThisMonth">Count of created issues in month.</param>
    /// <param name="movedToDoneThisMonth">Count of issues moved to done in month.</param>
    /// <param name="rejectedThisMonth">Count of issues moved to rejected in month.</param>
    /// <param name="finishedThisMonth">Count of finished issues in month (done + rejected).</param>
    /// <param name="openIssues">Issues created in the selected period and not moved to done/rejected in that period.</param>
    /// <param name="doneIssues">Issues moved to done in the selected period.</param>
    /// <param name="rejectedIssues">Issues moved to rejected in the selected period.</param>
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
    /// Shows issue counts grouped by status and issue type, excluding done/rejected statuses.
    /// </summary>
    /// <param name="statusSummaries">Issue counts by status and issue type.</param>
    /// <param name="doneStatusName">Done status name excluded from metric.</param>
    /// <param name="rejectStatusName">Optional reject status name excluded from metric.</param>
    void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    /// <summary>
    /// Shows path group details.
    /// </summary>
    /// <param name="groups">Path groups.</param>
    void ShowPathGroups(IReadOnlyList<PathGroup> groups);

    /// <summary>
    /// Shows final execution summary including total duration and Jira HTTP telemetry.
    /// </summary>
    /// <param name="totalDuration">Total application run duration.</param>
    /// <param name="requestTelemetry">Aggregated Jira HTTP telemetry.</param>
    void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry);

    /// <summary>
    /// Shows failed issue table.
    /// </summary>
    /// <param name="failures">Failures.</param>
    void ShowFailures(IReadOnlyList<LoadFailure> failures);
}
