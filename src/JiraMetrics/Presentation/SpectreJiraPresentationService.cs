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
        ReportSectionsPresenter = new SpectreReportSectionsPresenter(
            showTimeCalculationsInHoursOnly,
            runContext,
            ProgressPresenter);
    }

    /// <summary>
    /// Gets the presenter that owns issue-loading and pending-operation progress state.
    /// </summary>
    internal SpectreIssueLoadingProgressPresenter ProgressPresenter { get; }

    /// <summary>
    /// Gets the presenter responsible for report sections, analysis, and diagnostics.
    /// </summary>
    internal SpectreReportSectionsPresenter ReportSectionsPresenter { get; }

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
        => ReportSectionsPresenter.ShowDoneIssuesTable(issues, doneStatusName);

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
        => ReportSectionsPresenter.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
        => ReportSectionsPresenter.ShowRejectedIssuesTable(issues, rejectStatusName);

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
        => ReportSectionsPresenter.ShowPathGroupsSummary(summary);

    /// <inheritdoc />
    public void ShowReleaseReportLoadingStarted() => ReportSectionsPresenter.ShowReleaseReportLoadingStarted();

    /// <inheritdoc />
    public void ShowGlobalIncidentsReportLoadingStarted() => ReportSectionsPresenter.ShowGlobalIncidentsReportLoadingStarted();

    /// <inheritdoc />
    public void ShowArchTasksReportLoadingStarted() => ReportSectionsPresenter.ShowArchTasksReportLoadingStarted();

    /// <inheritdoc />
    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases)
        => ReportSectionsPresenter.ShowReleaseReport(settings, reportPeriod, releases);

    /// <inheritdoc />
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
        => ReportSectionsPresenter.ShowArchTasksReport(settings, tasks);

    /// <inheritdoc />
    public void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents)
        => ReportSectionsPresenter.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingStarted() => ReportSectionsPresenter.ShowAllTasksRatioLoadingStarted();

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
        => ReportSectionsPresenter.ShowAllTasksRatioLoadingCompleted(snapshot);

    /// <inheritdoc />
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
        => ReportSectionsPresenter.ShowAllTasksRatio(customFieldName, customFieldValue, snapshot);

    /// <inheritdoc />
    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
        => ReportSectionsPresenter.ShowBugRatioLoadingStarted(bugIssueNames);

    /// <inheritdoc />
    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
        => ReportSectionsPresenter.ShowBugRatioLoadingCompleted(snapshot);

    /// <inheritdoc />
    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
        => ReportSectionsPresenter.ShowBugRatio(
            bugIssueNames,
            customFieldName,
            customFieldValue,
            snapshot);

    /// <inheritdoc />
    public void ShowTestCoverageLoadingStarted(TestCoverageSettings settings)
        => ReportSectionsPresenter.ShowTestCoverageLoadingStarted(settings);

    /// <inheritdoc />
    public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
        => ReportSectionsPresenter.ShowTestCoverage(settings, snapshot);

    /// <inheritdoc />
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
        => ReportSectionsPresenter.ShowOpenIssuesByStatusSummary(
            statusSummaries,
            doneStatusName,
            rejectStatusName);

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
        => ReportSectionsPresenter.ShowPathGroups(groups);

    /// <inheritdoc />
    public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
    {
        ArgumentNullException.ThrowIfNull(requestTelemetry);
        StopAllLoaders();
        _statusSection.ShowExecutionSummary(totalDuration, requestTelemetry);
    }

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
        => ReportSectionsPresenter.ShowFailures(failures);

    /// <inheritdoc />
    public void ShowOptionalSectionFailures(IReadOnlyList<OptionalSectionLoadFailure> failures)
    {
        StopAllLoaders();
        ReportSectionsPresenter.ShowOptionalSectionFailures(failures);
    }

    private void StopAllLoaders() => ProgressPresenter.Stop();
    private readonly SpectreStatusSection _statusSection;
}

