using System.Diagnostics;
using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JiraMetrics.Logic;

/// <summary>
/// Default application workflow implementation.
/// </summary>
public sealed class JiraApplication : IJiraApplication
{
    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraStatusPresenter _statusPresenter;
    private readonly IJiraReportSectionsPresenter _reportSectionsPresenter;
    private readonly IJiraAnalysisPresenter _analysisPresenter;
    private readonly IJiraDiagnosticsPresenter _diagnosticsPresenter;
    private readonly IPdfReportRenderer _pdfReportRenderer;
    private readonly IJiraRequestTelemetryCollector _requestTelemetryCollector;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="dataFacade">Application data facade.</param>
    /// <param name="analysisFacade">Application analysis facade.</param>
    /// <param name="presentationService">Presentation service.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    internal JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraPresentationService presentationService,
        IPdfReportRenderer pdfReportRenderer)
        : this(
            settings,
            dataFacade,
            analysisFacade,
            presentationService,
            presentationService,
            presentationService,
            presentationService,
            pdfReportRenderer,
            NoOpJiraRequestTelemetryCollector.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="dataFacade">Application data facade.</param>
    /// <param name="analysisFacade">Application analysis facade.</param>
    /// <param name="presentationService">Presentation service.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    /// <param name="requestTelemetryCollector">Jira transport telemetry collector.</param>
    internal JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraPresentationService presentationService,
        IPdfReportRenderer pdfReportRenderer,
        IJiraRequestTelemetryCollector requestTelemetryCollector)
        : this(
            settings,
            dataFacade,
            analysisFacade,
            presentationService,
            presentationService,
            presentationService,
            presentationService,
            pdfReportRenderer,
            requestTelemetryCollector)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraStatusPresenter statusPresenter,
        IJiraReportSectionsPresenter reportSectionsPresenter,
        IJiraAnalysisPresenter analysisPresenter,
        IJiraDiagnosticsPresenter diagnosticsPresenter,
        IPdfReportRenderer pdfReportRenderer)
        : this(
            settings,
            dataFacade,
            analysisFacade,
            statusPresenter,
            reportSectionsPresenter,
            analysisPresenter,
            diagnosticsPresenter,
            pdfReportRenderer,
            NoOpJiraRequestTelemetryCollector.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraStatusPresenter statusPresenter,
        IJiraReportSectionsPresenter reportSectionsPresenter,
        IJiraAnalysisPresenter analysisPresenter,
        IJiraDiagnosticsPresenter diagnosticsPresenter,
        IPdfReportRenderer pdfReportRenderer,
        IJiraRequestTelemetryCollector requestTelemetryCollector)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(statusPresenter);
        ArgumentNullException.ThrowIfNull(reportSectionsPresenter);
        ArgumentNullException.ThrowIfNull(analysisPresenter);
        ArgumentNullException.ThrowIfNull(diagnosticsPresenter);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);

        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _statusPresenter = statusPresenter;
        _reportSectionsPresenter = reportSectionsPresenter;
        _analysisPresenter = analysisPresenter;
        _diagnosticsPresenter = diagnosticsPresenter;
        _pdfReportRenderer = pdfReportRenderer;
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
            _statusPresenter.ShowSpacer();
            _statusPresenter.ShowExecutionSummary(
                totalStopwatch.Elapsed,
                _requestTelemetryCollector.GetSummary());
        }
    }

    private async Task<JiraApplicationReportData?> TryLoadReportDataAsync(CancellationToken cancellationToken)
    {
        _statusPresenter.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _statusPresenter.ShowSpacer();

        ShowOptionalReportLoadingStarted();
        _reportSectionsPresenter.ShowAllTasksRatioLoadingStarted();

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
        ArgumentNullException.ThrowIfNull(reportData);

        var reportContext = reportData.ReportContext;
        _statusPresenter.ShowReportHeader(_settings, new ItemCount(reportContext.IssueKeys.Count));

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
            _statusPresenter.ShowSpacer();
            _diagnosticsPresenter.ShowFailures(loadResult.Failures);
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        _statusPresenter.ShowAuthenticationStarted();

        try
        {
            var user = await _dataFacade.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
            _statusPresenter.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _statusPresenter.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }
    }

    private void ShowOptionalReportLoadingStarted()
    {
        if (_settings.ReleaseReport is not null)
        {
            _reportSectionsPresenter.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportSectionsPresenter.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        ArgumentNullException.ThrowIfNull(reportContext);

        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _reportSectionsPresenter.ShowSpacer();
            _reportSectionsPresenter.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _reportSectionsPresenter.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _reportSectionsPresenter.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _reportSectionsPresenter.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _reportSectionsPresenter.ShowSpacer();
        }
    }

    private Task<IssueRatioSnapshot?>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        _reportSectionsPresenter.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.BugIssueNames,
                cancellationToken));
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowAllTasksRatioAsync(Task<IssueRatioSnapshot?> allTasksRatioTask)
    {
        ArgumentNullException.ThrowIfNull(allTasksRatioTask);

        var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        _reportSectionsPresenter.ShowAllTasksRatioLoadingCompleted(
            allTasksRatio.CreatedThisMonth,
            allTasksRatio.MovedToDoneThisMonth,
            allTasksRatio.RejectedThisMonth,
            allTasksRatio.FinishedThisMonth);
        _reportSectionsPresenter.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            allTasksRatio.CreatedThisMonth,
            allTasksRatio.OpenThisMonth,
            allTasksRatio.MovedToDoneThisMonth,
            allTasksRatio.RejectedThisMonth,
            allTasksRatio.FinishedThisMonth);
        _reportSectionsPresenter.ShowSpacer();

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

        _reportSectionsPresenter.ShowBugRatioLoadingCompleted(
            bugRatio.CreatedThisMonth,
            bugRatio.MovedToDoneThisMonth,
            bugRatio.RejectedThisMonth,
            bugRatio.FinishedThisMonth);
        _reportSectionsPresenter.ShowBugRatio(
            _settings.BugIssueNames,
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            bugRatio.CreatedThisMonth,
            bugRatio.MovedToDoneThisMonth,
            bugRatio.RejectedThisMonth,
            bugRatio.FinishedThisMonth,
            bugRatio.OpenIssues,
            bugRatio.DoneIssues,
            bugRatio.RejectedIssues);
        _reportSectionsPresenter.ShowSpacer();

        return bugRatio;
    }

    private async Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        JiraReportContext reportContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportContext);

        return await _dataFacade.LoadIssueTimelinesAsync(
            reportContext.IssueKeys,
            reportContext.RejectIssueKeys,
            cancellationToken).ConfigureAwait(false);
    }

    private JiraIssueAnalysisResult AnalyzeLoadedIssues(IssueTimelineLoadResult loadResult)
    {
        ArgumentNullException.ThrowIfNull(loadResult);

        _statusPresenter.ShowProcessingStep(
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
        ArgumentNullException.ThrowIfNull(reportContext);

        if (reportContext.IssueKeys.Count > 0)
        {
            return false;
        }

        _statusPresenter.ShowNoIssuesMatchedFilter();
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        return true;
    }

    private bool TryHandleNoLoadedIssues(
        IssueTimelineLoadResult loadResult,
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        ArgumentNullException.ThrowIfNull(loadResult);
        ArgumentNullException.ThrowIfNull(reportContext);

        if (loadResult.DoneIssues.Count > 0)
        {
            return false;
        }

        _statusPresenter.ShowNoIssuesLoaded();
        _diagnosticsPresenter.ShowFailures(loadResult.Failures);
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        return true;
    }

    private bool TryHandleUnsuccessfulAnalysis(
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures,
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(failures);
        ArgumentNullException.ThrowIfNull(reportContext);

        switch (analysis.Outcome)
        {
            case JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter:
                _statusPresenter.ShowNoIssuesMatchedFilter();
                _diagnosticsPresenter.ShowFailures(failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                return true;
            case JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage:
                _statusPresenter.ShowNoIssuesMatchedRequiredStage();
                _diagnosticsPresenter.ShowFailures(failures);
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
        ArgumentNullException.ThrowIfNull(analysis);

        _statusPresenter.ShowProcessingStep(
            "Calculating transition metrics and percentiles...");
        _analysisPresenter.ShowDoneIssuesTable(analysis.DoneIssues, _settings.DoneStatusName);
        _analysisPresenter.ShowSpacer();
        _analysisPresenter.ShowDoneDaysAtWork75PerType(
            analysis.DoneDaysAtWork75PerType,
            _settings.DoneStatusName);
        _analysisPresenter.ShowSpacer();

        if (_settings.RejectStatusName is { } rejectStatusName)
        {
            _analysisPresenter.ShowRejectedIssuesTable(analysis.RejectedIssues, rejectStatusName);
            _analysisPresenter.ShowSpacer();
        }

        _statusPresenter.ShowProcessingStep("Building path groups...");
        var pathSummary = analysis.PathSummary
            ?? throw new InvalidOperationException("Path summary is required for successful analysis.");
        _analysisPresenter.ShowPathGroupsSummary(pathSummary);
        _analysisPresenter.ShowSpacer();
        _analysisPresenter.ShowPathGroups(analysis.PathGroups);
    }

    private void RenderPdfReport(
        JiraApplicationReportData reportData,
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(failures);

        _statusPresenter.ShowProcessingStep("Rendering PDF report...");
        _pdfReportRenderer.RenderReport(JiraPdfReportData.Create(
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
        ArgumentNullException.ThrowIfNull(reportContext);

        if (openIssuesSummaryShown || !_settings.ShowGeneralStatistics)
        {
            return;
        }

        _diagnosticsPresenter.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _diagnosticsPresenter.ShowSpacer();
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
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }
}

