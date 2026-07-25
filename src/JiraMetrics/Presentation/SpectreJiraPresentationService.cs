using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Presentation;

/// <summary>
/// Spectre.Console-based presentation service.
/// </summary>
public sealed class SpectreJiraPresentationService : IJiraPresentationService, IReportOutputPresenter
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreJiraPresentationService"/> class.
    /// </summary>
    public SpectreJiraPresentationService()
        : this(
            showTimeCalculationsInHoursOnly: false,
            ReportRunContext.Create(TimeProvider.System))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreJiraPresentationService"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    public SpectreJiraPresentationService(IOptions<AppSettings> settings)
        : this(settings is null
            ? throw new ArgumentNullException(nameof(settings))
            : settings.Value.ShowTimeCalculationsInHoursOnly,
            ReportRunContext.Create(TimeProvider.System))
    {
    }

    /// <summary>
    /// Initializes a new instance with a context shared by the report run.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="runContext">Current report run context.</param>
    public SpectreJiraPresentationService(
        IOptions<AppSettings> settings,
        ReportRunContext runContext)
        : this(
            settings is null
                ? throw new ArgumentNullException(nameof(settings))
                : settings.Value.ShowTimeCalculationsInHoursOnly,
            runContext)
    {
    }

    private SpectreJiraPresentationService(
        bool showTimeCalculationsInHoursOnly,
        ReportRunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(runContext);

        _statusSection = new SpectreStatusSection();
        ProgressPresenter = new SpectreIssueLoadingProgressPresenter(_statusSection);
        _ratioSection = new SpectreRatioSection();
        _releaseSection = new SpectreReleaseSection();
        _archTasksSection = new SpectreArchTasksSection(runContext.GeneratedAt);
        _globalIncidentsSection = new SpectreGlobalIncidentsSection(showTimeCalculationsInHoursOnly);
        _transitionSection = new SpectreTransitionSection(showTimeCalculationsInHoursOnly);
        _generalStatisticsSection = new SpectreGeneralStatisticsSection(runContext.GeneratedAt);
        _failuresSection = new SpectreFailuresSection();
    }

    internal SpectreIssueLoadingProgressPresenter ProgressPresenter { get; }

    /// <inheritdoc />
    public void ShowAuthenticationStarted() => _statusSection.ShowAuthenticationStarted();

    /// <inheritdoc />
    public void ShowAuthenticationSucceeded(JiraAuthUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _statusSection.ShowAuthenticationSucceeded(user);
    }

    /// <inheritdoc />
    public void ShowAuthenticationFailed(ErrorMessage errorMessage)
    {
        _statusSection.ShowAuthenticationFailed(errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter) =>
        _statusSection.ShowReportPeriodContext(reportPeriod, createdAfter);

    /// <inheritdoc />
    public void ShowIssueSearchFailed(ErrorMessage errorMessage)
    {
        StopAllLoaders();
        _statusSection.ShowIssueSearchFailed(errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
    {
        ArgumentNullException.ThrowIfNull(settings);
        StopAllLoaders();
        _statusSection.ShowReportHeader(settings, issueCount);
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedFilter()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesMatchedFilter();
    }

    /// <inheritdoc />
    public void ShowIssueLoadingStarted(ItemCount totalIssues)
        => ProgressPresenter.ShowIssueLoadingStarted(totalIssues);

    /// <inheritdoc />
    public void ShowIssueLoaded(IssueKey issueKey) => ProgressPresenter.ShowIssueLoaded(issueKey);

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey) => ProgressPresenter.ShowIssueFailed(issueKey);

    /// <inheritdoc />
    public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues)
        => ProgressPresenter.ShowIssueLoadingCompleted(loadedIssues, failedIssues);

    /// <inheritdoc />
    public void ShowProcessingStep(string message)
    {
        StopAllLoaders();
        _statusSection.ShowProcessingStep(message);
    }

    /// <inheritdoc />
    public void ShowReportSaved(ReportOutputFormat format, string outputPath)
    {
        StopAllLoaders();
        _statusSection.ShowReportSaved(format, outputPath);
    }

    /// <inheritdoc />
    public void ShowReportGenerationFailed(ReportOutputFormat format, ErrorMessage errorMessage)
    {
        StopAllLoaders();
        _statusSection.ShowReportGenerationFailed(format, errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportOpenFailed(
        ReportOutputFormat format,
        string outputPath,
        ErrorMessage errorMessage)
    {
        StopAllLoaders();
        _statusSection.ShowReportOpenFailed(format, outputPath, errorMessage);
    }

    /// <inheritdoc />
    public void ShowSpacer()
    {
        StopAllLoaders();
        _statusSection.ShowSpacer();
    }

    /// <inheritdoc />
    public void ShowNoIssuesLoaded()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesLoaded();
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedRequiredStage()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesMatchedRequiredStage();
    }

    /// <inheritdoc />
    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopAllLoaders();
        _transitionSection.ShowDoneIssuesTable(issues, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        StopAllLoaders();
        _transitionSection.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopAllLoaders();
        _transitionSection.ShowRejectedIssuesTable(issues, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        StopAllLoaders();
        _transitionSection.ShowPathGroupsSummary(summary);
    }

    /// <inheritdoc />
    public void ShowReleaseReportLoadingStarted() => ProgressPresenter.ShowPending("Loading release report data...");

    /// <inheritdoc />
    public void ShowGlobalIncidentsReportLoadingStarted() => ProgressPresenter.ShowPending("Loading global incidents report data...");

    /// <inheritdoc />
    public void ShowArchTasksReportLoadingStarted() => ProgressPresenter.ShowPending("Loading architecture tasks report data...");

    /// <inheritdoc />
    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(releases);
        StopAllLoaders();
        _releaseSection.ShowReleaseReport(settings, reportPeriod, releases);
    }

    /// <inheritdoc />
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);
        StopAllLoaders();
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
        StopAllLoaders();
        _globalIncidentsSection.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingStarted() => ProgressPresenter.ShowPending("Loading all tasks ratio data...");

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowAllTasksRatioLoadingCompleted(snapshot);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowAllTasksRatio(
            customFieldName,
            customFieldValue,
            snapshot);
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);

        var bugTypes = bugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        ProgressPresenter.ShowPending($"Loading bug ratio data for: {bugTypes}");
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
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
        StopAllLoaders();
        _ratioSection.ShowBugRatio(
            bugIssueNames,
            customFieldName,
            customFieldValue,
            snapshot);
    }

    /// <inheritdoc />
    public void ShowTestCoverageLoadingStarted(TestCoverageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var issueTypes = string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value));
        ProgressPresenter.ShowPending($"Loading automated test coverage for: {issueTypes}");
    }

    /// <inheritdoc />
    public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowTestCoverage(settings, snapshot);
    }

    /// <inheritdoc />
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(statusSummaries);
        StopAllLoaders();
        _generalStatisticsSection.ShowOpenIssuesByStatusSummary(statusSummaries, doneStatusName, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        StopAllLoaders();
        _transitionSection.ShowPathGroups(groups);
    }

    /// <inheritdoc />
    public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
    {
        ArgumentNullException.ThrowIfNull(requestTelemetry);
        StopAllLoaders();
        _statusSection.ShowExecutionSummary(totalDuration, requestTelemetry);
    }

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        StopAllLoaders();
        _failuresSection.ShowFailures(failures);
    }

    private void StopAllLoaders() => ProgressPresenter.Stop();
    private readonly SpectreStatusSection _statusSection;
    private readonly SpectreRatioSection _ratioSection;
    private readonly SpectreReleaseSection _releaseSection;
    private readonly SpectreArchTasksSection _archTasksSection;
    private readonly SpectreGlobalIncidentsSection _globalIncidentsSection;
    private readonly SpectreTransitionSection _transitionSection;
    private readonly SpectreGeneralStatisticsSection _generalStatisticsSection;
    private readonly SpectreFailuresSection _failuresSection;
}

