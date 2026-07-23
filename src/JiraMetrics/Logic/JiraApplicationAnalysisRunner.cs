using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Executes timeline loading, analysis, presentation, and report rendering for a prepared report.
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

        if (reportContext.IssueKeys.Count == 0)
        {
            _statusPresenter.ShowNoIssuesMatchedFilter();
            CompleteWithoutTransitionAnalysis(
                reportData,
                failures: [],
                successfulCount: new ItemCount(0));
            return;
        }

        var loadResult = await _dataFacade.LoadIssueTimelinesAsync(
            reportContext.IssueKeys,
            reportContext.RejectIssueKeys,
            cancellationToken).ConfigureAwait(false);
        if (loadResult.DoneIssues.Count == 0)
        {
            _statusPresenter.ShowNoIssuesLoaded();
            _diagnosticsPresenter.ShowFailures(loadResult.Failures);
            CompleteWithoutTransitionAnalysis(
                reportData,
                loadResult.Failures,
                loadResult.LoadedIssueCount);
            return;
        }

        var analysis = AnalyzeLoadedIssues(loadResult);
        if (analysis.Outcome != JiraIssueAnalysisOutcome.Success)
        {
            PresentUnsuccessfulAnalysis(analysis.Outcome);
            _diagnosticsPresenter.ShowFailures(loadResult.Failures);
            CompleteWithoutTransitionAnalysis(
                reportData,
                loadResult.Failures,
                loadResult.LoadedIssueCount);
            return;
        }

        PresentSuccessfulAnalysis(analysis);
        ShowOpenIssuesSummary(reportContext);
        RenderReport(reportData, loadResult.Failures, analysis);

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

    private void PresentUnsuccessfulAnalysis(JiraIssueAnalysisOutcome outcome)
    {
        switch (outcome)
        {
            case JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter:
                _statusPresenter.ShowNoIssuesMatchedFilter();
                break;
            case JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage:
                _statusPresenter.ShowNoIssuesMatchedRequiredStage();
                break;
            case JiraIssueAnalysisOutcome.Success:
                throw new InvalidOperationException("Successful analysis cannot be presented as unsuccessful.");
            default:
                throw new InvalidOperationException($"Unsupported analysis outcome: {outcome}.");
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

    private void CompleteWithoutTransitionAnalysis(
        JiraApplicationReportData reportData,
        IReadOnlyList<LoadFailure> failures,
        ItemCount successfulCount)
    {
        ShowOpenIssuesSummary(reportData.ReportContext);
        RenderReport(reportData, failures, analysis: null, successfulCount);
    }

    private void RenderReport(
        JiraApplicationReportData reportData,
        IReadOnlyList<LoadFailure> failures,
        JiraIssueAnalysisResult? analysis,
        ItemCount successfulCount = default)
    {
        _statusPresenter.ShowProcessingStep("Rendering reports...");

        var renderedReport = analysis is null
            ? JiraReportData.CreateWithoutTransitionAnalysis(
                _runContext,
                _settings,
                reportData.ReportContext,
                reportData.AllTasksRatio,
                reportData.BugRatio,
                reportData.InternalIncidents,
                reportData.TestCoverage,
                failures,
                successfulCount,
                matchedStageCount: new ItemCount(0))
            : JiraReportData.Create(
                _runContext,
                _settings,
                reportData.ReportContext,
                reportData.AllTasksRatio,
                reportData.BugRatio,
                reportData.InternalIncidents,
                reportData.TestCoverage,
                analysis,
                failures);

        _reportPipeline.RenderReport(renderedReport);
    }

    private void ShowOpenIssuesSummary(JiraReportContext reportContext)
    {
        if (!_settings.ShowGeneralStatistics)
        {
            return;
        }

        _diagnosticsPresenter.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _statusPresenter.ShowSpacer();
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
