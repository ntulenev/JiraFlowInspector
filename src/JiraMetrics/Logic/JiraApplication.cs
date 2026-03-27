using System.Diagnostics;
using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Logic;

/// <summary>
/// Default application workflow implementation.
/// </summary>
public sealed class JiraApplication : IJiraApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="dataFacade">Application data facade.</param>
    /// <param name="analysisFacade">Application analysis facade.</param>
    /// <param name="reportingFacade">Application reporting facade.</param>
    /// <param name="requestTelemetryCollector">Jira transport telemetry collector.</param>
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraApplicationReportingFacade reportingFacade,
        IJiraRequestTelemetryCollector requestTelemetryCollector)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(reportingFacade);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);

        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _reportingFacade = reportingFacade;
        _requestTelemetryCollector = requestTelemetryCollector;
    }

    /// <summary>
    /// Executes the application flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _requestTelemetryCollector.Reset();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            var reportData = await TryLoadReportDataAsync(cancellationToken).ConfigureAwait(false);
            if (reportData is null)
            {
                return;
            }

            await RunAnalysisAndRenderingAsync(reportData, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            totalStopwatch.Stop();
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowExecutionSummary(
                totalStopwatch.Elapsed,
                _requestTelemetryCollector.GetSummary());
        }
    }

    private async Task<JiraApplicationReportData?> TryLoadReportDataAsync(CancellationToken cancellationToken)
    {
        _reportingFacade.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _reportingFacade.ShowSpacer();

        ShowOptionalReportLoadingStarted();
        _reportingFacade.ShowAllTasksRatioLoadingStarted();

        var reportContextTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadReportContextAsync(_settings, cancellationToken));
        var allTasksRatioTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(_settings, [], cancellationToken));
        var bugRatioTask = StartBugRatioLoading(cancellationToken);

        var reportContext = await reportContextTask.ConfigureAwait(false);
        if (reportContext is null)
        {
            return null;
        }

        ShowOptionalReports(reportContext);

        var allTasksRatio = await LoadAndShowAllTasksRatioAsync(allTasksRatioTask).ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        var bugRatio = await LoadAndShowBugRatioAsync(bugRatioTask).ConfigureAwait(false);
        if (bugRatioTask is not null && bugRatio is null)
        {
            return null;
        }

        return new JiraApplicationReportData(reportContext, allTasksRatio, bugRatio);
    }

    private async Task RunAnalysisAndRenderingAsync(
        JiraApplicationReportData reportData,
        CancellationToken cancellationToken)
    {
        var reportContext = reportData.ReportContext;
        _reportingFacade.ShowReportHeader(_settings, new ItemCount(reportContext.IssueKeys.Count));

        var openIssuesSummaryShown = false;
        if (TryHandleEmptyReportContext(reportContext, ref openIssuesSummaryShown))
        {
            return;
        }

        var loadResult = await LoadIssueTimelinesAsync(reportContext, cancellationToken).ConfigureAwait(false);
        if (TryHandleNoLoadedIssues(loadResult, reportContext, ref openIssuesSummaryShown))
        {
            return;
        }

        var analysis = AnalyzeLoadedIssues(loadResult);
        if (TryHandleUnsuccessfulAnalysis(analysis, loadResult.Failures, reportContext, ref openIssuesSummaryShown))
        {
            return;
        }

        PresentSuccessfulAnalysis(analysis);
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        RenderPdfReport(reportData, analysis, loadResult.Failures);

        if (loadResult.Failures.Count > 0)
        {
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowFailures(loadResult.Failures);
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        _reportingFacade.ShowAuthenticationStarted();

        try
        {
            var user = await _dataFacade.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
            _reportingFacade.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _reportingFacade.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }
    }

    private void ShowOptionalReportLoadingStarted()
    {
        if (_settings.ReleaseReport is not null)
        {
            _reportingFacade.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportingFacade.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportingFacade.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _reportingFacade.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _reportingFacade.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _reportingFacade.ShowSpacer();
        }
    }

    private Task<IssueRatioSnapshot?>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.BugIssueNames,
                cancellationToken));
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowAllTasksRatioAsync(Task<IssueRatioSnapshot?> allTasksRatioTask)
    {
        var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowAllTasksRatioLoadingCompleted(allTasksRatio);
        _reportingFacade.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            allTasksRatio);
        _reportingFacade.ShowSpacer();

        return allTasksRatio;
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowBugRatioAsync(Task<IssueRatioSnapshot?>? bugRatioTask)
    {
        if (bugRatioTask is null)
        {
            return null;
        }

        var bugRatio = await bugRatioTask.ConfigureAwait(false);
        if (bugRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingCompleted(bugRatio);
        _reportingFacade.ShowBugRatio(
            _settings.BugIssueNames,
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            bugRatio);
        _reportingFacade.ShowSpacer();

        return bugRatio;
    }

    private async Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        JiraReportContext reportContext,
        CancellationToken cancellationToken)
    {
        return await _dataFacade.LoadIssueTimelinesAsync(
            reportContext.IssueKeys,
            reportContext.RejectIssueKeys,
            cancellationToken).ConfigureAwait(false);
    }

    private JiraIssueAnalysisResult AnalyzeLoadedIssues(IssueTimelineLoadResult loadResult)
    {
        _reportingFacade.ShowProcessingStep(
            "Applying issue type and required-stage filters...");
        return _analysisFacade.Analyze(
            loadResult.DoneIssues,
            loadResult.RejectIssues,
            loadResult.Failures,
            _settings);
    }

    private bool TryHandleEmptyReportContext(
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        if (reportContext.IssueKeys.Count > 0)
        {
            return false;
        }

        _reportingFacade.ShowNoIssuesMatchedFilter();
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        return true;
    }

    private bool TryHandleNoLoadedIssues(
        IssueTimelineLoadResult loadResult,
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        if (loadResult.DoneIssues.Count > 0)
        {
            return false;
        }

        _reportingFacade.ShowNoIssuesLoaded();
        _reportingFacade.ShowFailures(loadResult.Failures);
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        return true;
    }

    private bool TryHandleUnsuccessfulAnalysis(
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures,
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        switch (analysis.Outcome)
        {
            case JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter:
                _reportingFacade.ShowNoIssuesMatchedFilter();
                _reportingFacade.ShowFailures(failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                return true;
            case JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage:
                _reportingFacade.ShowNoIssuesMatchedRequiredStage();
                _reportingFacade.ShowFailures(failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                return true;
            case JiraIssueAnalysisOutcome.Success:
                return false;
            default:
                throw new InvalidOperationException($"Unsupported analysis outcome: {analysis.Outcome}.");
        }
    }

    private void PresentSuccessfulAnalysis(JiraIssueAnalysisResult analysis)
    {
        _reportingFacade.ShowProcessingStep(
            "Calculating transition metrics and percentiles...");
        _reportingFacade.ShowDoneIssuesTable(analysis.DoneIssues, _settings.DoneStatusName);
        _reportingFacade.ShowSpacer();
        _reportingFacade.ShowDoneDaysAtWork75PerType(
            analysis.DoneDaysAtWork75PerType,
            _settings.DoneStatusName);
        _reportingFacade.ShowSpacer();

        if (_settings.RejectStatusName is { } rejectStatusName)
        {
            _reportingFacade.ShowRejectedIssuesTable(analysis.RejectedIssues, rejectStatusName);
            _reportingFacade.ShowSpacer();
        }

        _reportingFacade.ShowProcessingStep("Building path groups...");
        var pathSummary = analysis.PathSummary
            ?? throw new InvalidOperationException("Path summary is required for successful analysis.");
        _reportingFacade.ShowPathGroupsSummary(pathSummary);
        _reportingFacade.ShowSpacer();
        _reportingFacade.ShowPathGroups(analysis.PathGroups);
    }

    private void RenderPdfReport(
        JiraApplicationReportData reportData,
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures)
    {
        _reportingFacade.ShowProcessingStep("Rendering PDF report...");
        _reportingFacade.RenderReport(JiraPdfReportData.Create(
            _settings,
            reportData.ReportContext,
            reportData.AllTasksRatio,
            reportData.BugRatio,
            analysis,
            failures));
    }

    private void ShowOpenIssuesSummaryIfNeeded(
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        if (openIssuesSummaryShown || !_settings.ShowGeneralStatistics)
        {
            return;
        }

        _reportingFacade.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _reportingFacade.ShowSpacer();
        openIssuesSummaryShown = true;
    }

    private async Task<T?> TryLoadSearchDataAsync<T>(Func<Task<T>> loadAsync)
        where T : class
    {
        try
        {
            return await loadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }
    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraApplicationReportingFacade _reportingFacade;
    private readonly IJiraRequestTelemetryCollector _requestTelemetryCollector;
}

