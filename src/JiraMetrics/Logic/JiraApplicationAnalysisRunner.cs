using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Executes timeline loading, analysis, presentation, and PDF rendering for a prepared report.
/// </summary>
internal sealed class JiraApplicationAnalysisRunner : IJiraApplicationAnalysisRunner
{
    public JiraApplicationAnalysisRunner(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraApplicationReportingFacade reportingFacade)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(reportingFacade);

        _settings = settings;
        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _reportingFacade = reportingFacade;
    }

    public async Task RunAsync(
        JiraApplicationReportData reportData,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var reportContext = reportData.ReportContext;
        _reportingFacade.ShowReportHeader(_settings, new ItemCount(reportContext.IssueKeys.Count));

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
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowFailures(loadResult.Failures);
        }
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
        JiraApplicationReportData reportData,
        ref bool openIssuesSummaryShown)
    {
        var reportContext = reportData.ReportContext;
        if (reportContext.IssueKeys.Count > 0)
        {
            return false;
        }

        _reportingFacade.ShowNoIssuesMatchedFilter();
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

        _reportingFacade.ShowNoIssuesLoaded();
        _reportingFacade.ShowFailures(loadResult.Failures);
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
                _reportingFacade.ShowNoIssuesMatchedFilter();
                _reportingFacade.ShowFailures(loadResult.Failures);
                ShowOpenIssuesSummaryIfNeeded(reportContext, ref openIssuesSummaryShown);
                RenderPdfReportWithoutTransitionAnalysis(
                    reportData,
                    loadResult.Failures,
                    successfulCount: loadResult.LoadedIssueCount,
                    matchedStageCount: new ItemCount(0));
                return true;
            case JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage:
                _reportingFacade.ShowNoIssuesMatchedRequiredStage();
                _reportingFacade.ShowFailures(loadResult.Failures);
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

    private void RenderPdfReportWithoutTransitionAnalysis(
        JiraApplicationReportData reportData,
        IReadOnlyList<LoadFailure> failures,
        ItemCount successfulCount,
        ItemCount matchedStageCount)
    {
        _reportingFacade.ShowProcessingStep("Rendering PDF report...");
        _reportingFacade.RenderReport(JiraPdfReportData.CreateWithoutTransitionAnalysis(
            _settings,
            reportData.ReportContext,
            reportData.AllTasksRatio,
            reportData.BugRatio,
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

        _reportingFacade.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _reportingFacade.ShowSpacer();
        openIssuesSummaryShown = true;
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraApplicationReportingFacade _reportingFacade;
}
