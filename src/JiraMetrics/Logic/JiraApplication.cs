using System.Diagnostics;
using System.Text.Json;

using JiraMetrics.Abstractions;
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
    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraPresentationService _presentationService;
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
    public JiraApplication(
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
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraPresentationService presentationService,
        IPdfReportRenderer pdfReportRenderer,
        IJiraRequestTelemetryCollector requestTelemetryCollector)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(presentationService);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);

        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _presentationService = presentationService;
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

            _presentationService.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
            _presentationService.ShowSpacer();

            ShowOptionalReportLoadingStarted();
            _presentationService.ShowAllTasksRatioLoadingStarted();

            var reportContextTask = TryLoadSearchDataAsync(
                () => _dataFacade.LoadReportContextAsync(_settings, cancellationToken));
            var allTasksRatioTask = TryLoadSearchDataAsync(
                () => _dataFacade.LoadIssueRatioAsync(_settings, [], cancellationToken));
            Task<IssueRatioSnapshot?>? bugRatioTask = null;

            if (_settings.BugIssueNames.Count > 0)
            {
                _presentationService.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
                bugRatioTask = TryLoadSearchDataAsync(
                    () => _dataFacade.LoadIssueRatioAsync(
                        _settings,
                        _settings.BugIssueNames,
                        cancellationToken));
            }

            var reportContext = await reportContextTask.ConfigureAwait(false);
            if (reportContext is null)
            {
                return;
            }

            ShowOptionalReports(reportContext);

            var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
            if (allTasksRatio is null)
            {
                return;
            }

            _presentationService.ShowAllTasksRatioLoadingCompleted(
                allTasksRatio.CreatedThisMonth,
                allTasksRatio.MovedToDoneThisMonth,
                allTasksRatio.RejectedThisMonth,
                allTasksRatio.FinishedThisMonth);
            _presentationService.ShowAllTasksRatio(
                _settings.CustomFieldName,
                _settings.CustomFieldValue,
                allTasksRatio.CreatedThisMonth,
                allTasksRatio.OpenThisMonth,
                allTasksRatio.MovedToDoneThisMonth,
                allTasksRatio.RejectedThisMonth,
                allTasksRatio.FinishedThisMonth);
            _presentationService.ShowSpacer();

            IssueRatioSnapshot? bugRatio = null;
            if (bugRatioTask is not null)
            {
                bugRatio = await bugRatioTask.ConfigureAwait(false);
                if (bugRatio is null)
                {
                    return;
                }

                _presentationService.ShowBugRatioLoadingCompleted(
                    bugRatio.CreatedThisMonth,
                    bugRatio.MovedToDoneThisMonth,
                    bugRatio.RejectedThisMonth,
                    bugRatio.FinishedThisMonth);
                _presentationService.ShowBugRatio(
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
                _presentationService.ShowSpacer();
            }

            _presentationService.ShowReportHeader(_settings, new ItemCount(reportContext.IssueKeys.Count));

            var openIssuesSummaryShown = false;

            void ShowOpenIssuesSummary()
            {
                if (openIssuesSummaryShown || !_settings.ShowGeneralStatistics)
                {
                    return;
                }

                _presentationService.ShowOpenIssuesByStatusSummary(
                    reportContext.OpenIssuesByStatus,
                    _settings.DoneStatusName,
                    _settings.RejectStatusName);
                _presentationService.ShowSpacer();
                openIssuesSummaryShown = true;
            }

            if (reportContext.IssueKeys.Count == 0)
            {
                _presentationService.ShowNoIssuesMatchedFilter();
                ShowOpenIssuesSummary();
                return;
            }

            var loadResult = await _dataFacade.LoadIssueTimelinesAsync(
                reportContext.IssueKeys,
                reportContext.RejectIssueKeys,
                cancellationToken).ConfigureAwait(false);
            if (loadResult.DoneIssues.Count == 0)
            {
                _presentationService.ShowNoIssuesLoaded();
                _presentationService.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummary();
                return;
            }

            _presentationService.ShowProcessingStep(
                "Applying issue type and required-stage filters...");
            var analysis = _analysisFacade.Analyze(
                loadResult.DoneIssues,
                loadResult.RejectIssues,
                loadResult.Failures,
                _settings);

            if (analysis.Outcome == JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter)
            {
                _presentationService.ShowNoIssuesMatchedFilter();
                _presentationService.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummary();
                return;
            }

            if (analysis.Outcome == JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage)
            {
                _presentationService.ShowNoIssuesMatchedRequiredStage();
                _presentationService.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummary();
                return;
            }

            _presentationService.ShowProcessingStep(
                "Calculating transition metrics and percentiles...");
            _presentationService.ShowDoneIssuesTable(analysis.DoneIssues, _settings.DoneStatusName);
            _presentationService.ShowSpacer();
            _presentationService.ShowDoneDaysAtWork75PerType(
                analysis.DoneDaysAtWork75PerType,
                _settings.DoneStatusName);
            _presentationService.ShowSpacer();

            if (_settings.RejectStatusName is { } rejectStatusName)
            {
                _presentationService.ShowRejectedIssuesTable(analysis.RejectedIssues, rejectStatusName);
                _presentationService.ShowSpacer();
            }

            _presentationService.ShowProcessingStep("Building path groups...");
            _presentationService.ShowPathGroupsSummary(analysis.PathSummary!);
            _presentationService.ShowSpacer();
            _presentationService.ShowPathGroups(analysis.PathGroups);
            ShowOpenIssuesSummary();

            _presentationService.ShowProcessingStep("Rendering PDF report...");
            _pdfReportRenderer.RenderReport(JiraPdfReportData.Create(
                _settings,
                reportContext,
                allTasksRatio,
                bugRatio,
                analysis,
                loadResult.Failures));

            if (loadResult.Failures.Count > 0)
            {
                _presentationService.ShowSpacer();
                _presentationService.ShowFailures(loadResult.Failures);
            }
        }
        finally
        {
            totalStopwatch.Stop();
            _presentationService.ShowSpacer();
            _presentationService.ShowExecutionSummary(
                totalStopwatch.Elapsed,
                _requestTelemetryCollector.GetSummary());
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        _presentationService.ShowAuthenticationStarted();

        try
        {
            var user = await _dataFacade.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
            _presentationService.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _presentationService.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }
    }

    private void ShowOptionalReportLoadingStarted()
    {
        if (_settings.ReleaseReport is not null)
        {
            _presentationService.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _presentationService.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _presentationService.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        ArgumentNullException.ThrowIfNull(reportContext);

        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _presentationService.ShowSpacer();
            _presentationService.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _presentationService.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _presentationService.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _presentationService.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _presentationService.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _presentationService.ShowSpacer();
        }
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
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }

    private sealed class NoOpJiraRequestTelemetryCollector : IJiraRequestTelemetryCollector
    {
        public static NoOpJiraRequestTelemetryCollector Instance { get; } = new();

        public void Reset()
        {
        }

        public void Record(string method, Uri url, TimeSpan duration, int responseBytes, bool isRetry)
        {
        }

        public JiraRequestTelemetrySummary GetSummary() => new(0, 0, 0, TimeSpan.Zero, []);
    }
}
