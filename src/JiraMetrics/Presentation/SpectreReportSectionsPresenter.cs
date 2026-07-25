using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Presents report sections, transition analysis, and diagnostics in the console.
/// </summary>
internal sealed class SpectreReportSectionsPresenter :
    IJiraReportSectionsPresenter,
    IJiraAnalysisPresenter,
    IJiraDiagnosticsPresenter
{
    public SpectreReportSectionsPresenter(
        bool showTimeCalculationsInHoursOnly,
        ReportRunContext runContext,
        SpectreIssueLoadingProgressPresenter progressPresenter)
    {
        ArgumentNullException.ThrowIfNull(runContext);
        ArgumentNullException.ThrowIfNull(progressPresenter);

        _progressPresenter = progressPresenter;
        _ratioSection = new SpectreRatioSection();
        _releaseSection = new SpectreReleaseSection();
        _archTasksSection = new SpectreArchTasksSection(runContext.GeneratedAt);
        _globalIncidentsSection = new SpectreGlobalIncidentsSection(showTimeCalculationsInHoursOnly);
        _transitionSection = new SpectreTransitionSection(showTimeCalculationsInHoursOnly);
        _generalStatisticsSection = new SpectreGeneralStatisticsSection(runContext.GeneratedAt);
        _failuresSection = new SpectreFailuresSection();
    }

    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopProgress();
        _transitionSection.ShowDoneIssuesTable(issues, doneStatusName);
    }

    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        StopProgress();
        _transitionSection.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);
    }

    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopProgress();
        _transitionSection.ShowRejectedIssuesTable(issues, rejectStatusName);
    }

    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        StopProgress();
        _transitionSection.ShowPathGroupsSummary(summary);
    }

    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        StopProgress();
        _transitionSection.ShowPathGroups(groups);
    }

    public void ShowReleaseReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading release report data...");

    public void ShowGlobalIncidentsReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading global incidents report data...");

    public void ShowArchTasksReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading architecture tasks report data...");

    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(releases);
        StopProgress();
        _releaseSection.ShowReleaseReport(settings, reportPeriod, releases);
    }

    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);
        StopProgress();
        _archTasksSection.ShowArchTasksReport(settings, tasks);
    }

    public void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(incidents);
        StopProgress();
        _globalIncidentsSection.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);
    }

    public void ShowAllTasksRatioLoadingStarted() =>
        _progressPresenter.ShowPending("Loading all tasks ratio data...");

    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowAllTasksRatioLoadingCompleted(snapshot);
    }

    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowAllTasksRatio(customFieldName, customFieldValue, snapshot);
    }

    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        var bugTypes = bugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        _progressPresenter.ShowPending($"Loading bug ratio data for: {bugTypes}");
    }

    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowBugRatioLoadingCompleted(snapshot);
    }

    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowBugRatio(bugIssueNames, customFieldName, customFieldValue, snapshot);
    }

    public void ShowTestCoverageLoadingStarted(TestCoverageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var issueTypes = string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value));
        _progressPresenter.ShowPending($"Loading automated test coverage for: {issueTypes}");
    }

    public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowTestCoverage(settings, snapshot);
    }

    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(statusSummaries);
        StopProgress();
        _generalStatisticsSection.ShowOpenIssuesByStatusSummary(
            statusSummaries,
            doneStatusName,
            rejectStatusName);
    }

    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        StopProgress();
        _failuresSection.ShowFailures(failures);
    }

    public void ShowSpacer() => _progressPresenter.ShowSpacer();

    private void StopProgress() => _progressPresenter.Stop();

    private readonly SpectreIssueLoadingProgressPresenter _progressPresenter;
    private readonly SpectreRatioSection _ratioSection;
    private readonly SpectreReleaseSection _releaseSection;
    private readonly SpectreArchTasksSection _archTasksSection;
    private readonly SpectreGlobalIncidentsSection _globalIncidentsSection;
    private readonly SpectreTransitionSection _transitionSection;
    private readonly SpectreGeneralStatisticsSection _generalStatisticsSection;
    private readonly SpectreFailuresSection _failuresSection;
}
