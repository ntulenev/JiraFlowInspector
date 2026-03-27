using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Adapts presentation and PDF rendering services into one application-facing reporting facade.
/// </summary>
internal sealed class JiraApplicationReportingFacade : IJiraApplicationReportingFacade
{
    public JiraApplicationReportingFacade(
        IJiraPresentationService presentationService,
        IPdfReportRenderer pdfReportRenderer)
    {
        ArgumentNullException.ThrowIfNull(presentationService);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        _presentationService = presentationService;
        _pdfReportRenderer = pdfReportRenderer;
    }

    public void ShowAuthenticationStarted() => _presentationService.ShowAuthenticationStarted();

    public void ShowAuthenticationSucceeded(JiraAuthUser user) =>
        _presentationService.ShowAuthenticationSucceeded(user);

    public void ShowAuthenticationFailed(ErrorMessage errorMessage) =>
        _presentationService.ShowAuthenticationFailed(errorMessage);

    public void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter) =>
        _presentationService.ShowReportPeriodContext(reportPeriod, createdAfter);

    public void ShowIssueSearchFailed(ErrorMessage errorMessage) =>
        _presentationService.ShowIssueSearchFailed(errorMessage);

    public void ShowReportHeader(AppSettings settings, ItemCount issueCount) =>
        _presentationService.ShowReportHeader(settings, issueCount);

    public void ShowNoIssuesMatchedFilter() => _presentationService.ShowNoIssuesMatchedFilter();

    public void ShowProcessingStep(string message) => _presentationService.ShowProcessingStep(message);

    public void ShowSpacer() => ((IJiraStatusPresenter)_presentationService).ShowSpacer();

    public void ShowNoIssuesLoaded() => _presentationService.ShowNoIssuesLoaded();

    public void ShowNoIssuesMatchedRequiredStage() =>
        _presentationService.ShowNoIssuesMatchedRequiredStage();

    public void ShowExecutionSummary(
        TimeSpan totalDuration,
        JiraRequestTelemetrySummary requestTelemetry) =>
        _presentationService.ShowExecutionSummary(totalDuration, requestTelemetry);

    public void ShowReleaseReportLoadingStarted() => _presentationService.ShowReleaseReportLoadingStarted();

    public void ShowGlobalIncidentsReportLoadingStarted() =>
        _presentationService.ShowGlobalIncidentsReportLoadingStarted();

    public void ShowArchTasksReportLoadingStarted() => _presentationService.ShowArchTasksReportLoadingStarted();

    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases) =>
        _presentationService.ShowReleaseReport(settings, reportPeriod, releases);

    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks) =>
        _presentationService.ShowArchTasksReport(settings, tasks);

    public void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents) =>
        _presentationService.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);

    public void ShowAllTasksRatioLoadingStarted() => _presentationService.ShowAllTasksRatioLoadingStarted();

    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot) =>
        _presentationService.ShowAllTasksRatioLoadingCompleted(snapshot);

    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot) =>
        _presentationService.ShowAllTasksRatio(
            customFieldName,
            customFieldValue,
            snapshot);

    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames) =>
        _presentationService.ShowBugRatioLoadingStarted(bugIssueNames);

    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot) =>
        _presentationService.ShowBugRatioLoadingCompleted(snapshot);

    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot) =>
        _presentationService.ShowBugRatio(
            bugIssueNames,
            customFieldName,
            customFieldValue,
            snapshot);

    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName) =>
        _presentationService.ShowDoneIssuesTable(issues, doneStatusName);

    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName) =>
        _presentationService.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);

    public void ShowRejectedIssuesTable(
        IReadOnlyList<IssueTimeline> issues,
        StatusName rejectStatusName) =>
        _presentationService.ShowRejectedIssuesTable(issues, rejectStatusName);

    public void ShowPathGroupsSummary(PathGroupsSummary summary) =>
        _presentationService.ShowPathGroupsSummary(summary);

    public void ShowPathGroups(IReadOnlyList<PathGroup> groups) =>
        _presentationService.ShowPathGroups(groups);

    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName) =>
        _presentationService.ShowOpenIssuesByStatusSummary(
            statusSummaries,
            doneStatusName,
            rejectStatusName);

    public void ShowFailures(IReadOnlyList<LoadFailure> failures) =>
        _presentationService.ShowFailures(failures);

    public void RenderReport(JiraPdfReportData reportData) => _pdfReportRenderer.RenderReport(reportData);

    private readonly IJiraPresentationService _presentationService;
    private readonly IPdfReportRenderer _pdfReportRenderer;
}
