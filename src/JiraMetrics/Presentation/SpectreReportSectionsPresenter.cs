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
    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreReportSectionsPresenter"/> class.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether durations are presented only in hours.</param>
    /// <param name="runContext">Context shared by the current report run.</param>
    /// <param name="progressPresenter">Presenter controlling pending-operation output.</param>
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

    /// <inheritdoc />
    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopProgress();
        _transitionSection.ShowDoneIssuesTable(issues, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        StopProgress();
        _transitionSection.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopProgress();
        _transitionSection.ShowRejectedIssuesTable(issues, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        StopProgress();
        _transitionSection.ShowPathGroupsSummary(summary);
    }

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        StopProgress();
        _transitionSection.ShowPathGroups(groups);
    }

    /// <inheritdoc />
    public void ShowReleaseReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading release report data...");

    /// <inheritdoc />
    public void ShowGlobalIncidentsReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading global incidents report data...");

    /// <inheritdoc />
    public void ShowArchTasksReportLoadingStarted() =>
        _progressPresenter.ShowPending("Loading architecture tasks report data...");

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);
        StopProgress();
        _archTasksSection.ShowArchTasksReport(settings, tasks);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingStarted() =>
        _progressPresenter.ShowPending("Loading all tasks ratio data...");

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowAllTasksRatioLoadingCompleted(snapshot);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowAllTasksRatio(customFieldName, customFieldValue, snapshot);
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        var bugTypes = bugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        _progressPresenter.ShowPending($"Loading bug ratio data for: {bugTypes}");
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowBugRatioLoadingCompleted(snapshot);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ShowTestCoverageLoadingStarted(TestCoverageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var issueTypes = string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value));
        _progressPresenter.ShowPending($"Loading automated test coverage for: {issueTypes}");
    }

    /// <inheritdoc />
    public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);
        StopProgress();
        _ratioSection.ShowTestCoverage(settings, snapshot);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        StopProgress();
        _failuresSection.ShowFailures(failures);
    }

    /// <inheritdoc />
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
