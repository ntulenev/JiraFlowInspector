using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Executes timeline loading, analysis, presentation, and PDF rendering for a prepared report.
/// </summary>
internal sealed class JiraApplicationAnalysisRunner : IJiraApplicationAnalysisRunner
{
    internal JiraApplicationAnalysisRunner(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraStatusPresenter statusPresenter,
        IJiraAnalysisPresenter analysisPresenter,
        IJiraDiagnosticsPresenter diagnosticsPresenter,
        IJiraReportPipeline reportPipeline,
        ReportRunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(statusPresenter);
        ArgumentNullException.ThrowIfNull(analysisPresenter);
        ArgumentNullException.ThrowIfNull(diagnosticsPresenter);
        ArgumentNullException.ThrowIfNull(reportPipeline);
        ArgumentNullException.ThrowIfNull(runContext);

        _settings = settings;
        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _statusPresenter = statusPresenter;
        _analysisPresenter = analysisPresenter;
        _diagnosticsPresenter = diagnosticsPresenter;
        _reportPipeline = reportPipeline;
        _runContext = runContext;
    }

    public async Task RunAsync(
        JiraApplicationReportData reportData,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var reportContext = reportData.ReportContext;
        _statusPresenter.ShowReportHeader(_settings, new ItemCount(reportContext.IssueKeys.Count));

        var openIssuesSummaryShown = false;
        if (TryHandleEmptyReportContext(reportData, ref openIssuesSummaryShown))
        {
            return;
        }

        var loadResult = await _dataFacade.LoadIssueTimelinesAsync(
            reportContext.IssueKeys,
            reportContext.RejectIssueKeys,
            cancellationToken).ConfigureAwait(false);
        if (TryHandleNoLoadedIssues(reportData, loadResult, ref openIssuesSummaryShown))
        {
            return;
        }

        var analysis = AnalyzeLoadedIssues(loadResult);
        if (TryHandleUnsuccessfulAnalysis(reportData, analysis, loadResult, ref openIssuesSummaryShown))
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

    private JiraIssueAnalysisResult AnalyzeLoadedIssues(IssueTimelineLoadResult loadResult)
    {
        _statusPresenter.ShowProcessingStep(
            "Applying issue type and required-stage filters...");
        return _analysisFacade.Analyze(
            loadResult.DoneIssues,
            loadResult.RejectIssues,
            loadResult.Failures,
            _settings);
    }

    private bool TryHandleEmptyReportContext(
        JiraApplicationReportData reportData,
        ref bool openIssuesSummaryShown)
    {
        var reportContext = reportData.ReportContext;
        if (reportContext.IssueKeys.Count > 0)
        {
            return false;
        }

        _statusPresenter.ShowNoIssuesMatchedFilter();
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        RenderPdfReportWithoutTransitionAnalysis(
            reportData,
            failures: [],
            successfulCount: new ItemCount(0),
            matchedStageCount: new ItemCount(0));
        return true;
    }

    private bool TryHandleNoLoadedIssues(
        JiraApplicationReportData reportData,
        IssueTimelineLoadResult loadResult,
        ref bool openIssuesSummaryShown)
    {
        var reportContext = reportData.ReportContext;
        if (loadResult.DoneIssues.Count > 0)
        {
            return false;
        }

        _statusPresenter.ShowNoIssuesLoaded();
        _diagnosticsPresenter.ShowFailures(loadResult.Failures);
        ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
        RenderPdfReportWithoutTransitionAnalysis(
            reportData,
            loadResult.Failures,
            successfulCount: loadResult.LoadedIssueCount,
            matchedStageCount: new ItemCount(0));
        return true;
    }

    private bool TryHandleUnsuccessfulAnalysis(
        JiraApplicationReportData reportData,
        JiraIssueAnalysisResult analysis,
        IssueTimelineLoadResult loadResult,
        ref bool openIssuesSummaryShown)
    {
        var reportContext = reportData.ReportContext;

        switch (analysis.Outcome)
        {
            case JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter:
                _statusPresenter.ShowNoIssuesMatchedFilter();
                _diagnosticsPresenter.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                RenderPdfReportWithoutTransitionAnalysis(
                    reportData,
                    loadResult.Failures,
                    successfulCount: loadResult.LoadedIssueCount,
                    matchedStageCount: new ItemCount(0));
                return true;
            case JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage:
                _statusPresenter.ShowNoIssuesMatchedRequiredStage();
                _diagnosticsPresenter.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                RenderPdfReportWithoutTransitionAnalysis(
                    reportData,
                    loadResult.Failures,
                    successfulCount: loadResult.LoadedIssueCount,
                    matchedStageCount: new ItemCount(0));
                return true;
            case JiraIssueAnalysisOutcome.Success:
                return false;
            default:
                throw new InvalidOperationException($"Unsupported analysis outcome: {analysis.Outcome}.");
        }
    }

    private void PresentSuccessfulAnalysis(JiraIssueAnalysisResult analysis)
    {
        _statusPresenter.ShowProcessingStep(
            "Calculating transition metrics and percentiles...");
        _analysisPresenter.ShowDoneIssuesTable(analysis.DoneIssues, _settings.DoneStatusName);
        _statusPresenter.ShowSpacer();
        _analysisPresenter.ShowDoneDaysAtWork75PerType(
            analysis.DoneDaysAtWork75PerType,
            _settings.DoneStatusName);
        _statusPresenter.ShowSpacer();

        if (_settings.RejectStatusName is { } rejectStatusName)
        {
            _analysisPresenter.ShowRejectedIssuesTable(analysis.RejectedIssues, rejectStatusName);
            _statusPresenter.ShowSpacer();
        }

        _statusPresenter.ShowProcessingStep("Building path groups...");
        var pathSummary = analysis.PathSummary
            ?? throw new InvalidOperationException("Path summary is required for successful analysis.");
        _analysisPresenter.ShowPathGroupsSummary(pathSummary);
        _statusPresenter.ShowSpacer();
        _analysisPresenter.ShowPathGroups(analysis.PathGroups);
    }

    private void RenderPdfReport(
        JiraApplicationReportData reportData,
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures)
    {
        _statusPresenter.ShowProcessingStep("Rendering PDF report...");
        _reportPipeline.RenderReport(JiraReportData.Create(
            _runContext,
            _settings,
            reportData.ReportContext,
            reportData.AllTasksRatio,
            reportData.BugRatio,
            reportData.InternalIncidents,
            reportData.TestCoverage,
            analysis,
            failures));
    }

    private void RenderPdfReportWithoutTransitionAnalysis(
        JiraApplicationReportData reportData,
        IReadOnlyList<LoadFailure> failures,
        ItemCount successfulCount,
        ItemCount matchedStageCount)
    {
        _statusPresenter.ShowProcessingStep("Rendering PDF report...");
        _reportPipeline.RenderReport(JiraReportData.CreateWithoutTransitionAnalysis(
            _runContext,
            _settings,
            reportData.ReportContext,
            reportData.AllTasksRatio,
            reportData.BugRatio,
            reportData.InternalIncidents,
            reportData.TestCoverage,
            failures,
            successfulCount,
            matchedStageCount));
    }

    private void ShowOpenIssuesSummaryIfNeeded(
        JiraReportContext reportContext,
        ref bool openIssuesSummaryShown)
    {
        if (openIssuesSummaryShown || !_settings.ShowGeneralStatistics)
        {
            return;
        }

        _diagnosticsPresenter.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _statusPresenter.ShowSpacer();
        openIssuesSummaryShown = true;
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraStatusPresenter _statusPresenter;
    private readonly IJiraAnalysisPresenter _analysisPresenter;
    private readonly IJiraDiagnosticsPresenter _diagnosticsPresenter;
    private readonly IJiraReportPipeline _reportPipeline;
    private readonly ReportRunContext _runContext;
}
