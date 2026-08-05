using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Logic;

/// <summary>
/// Presents transition-analysis outcomes and renders the prepared report.
/// </summary>
internal sealed class JiraApplicationAnalysisRunner : IJiraApplicationAnalysisRunner
{
    internal JiraApplicationAnalysisRunner(
        AppSettings settings,
        JiraTransitionAnalysisRunner transitionAnalysisRunner,
        IJiraPresentationService presentation,
        JiraReportDataFactory reportDataFactory,
        IJiraReportPipeline reportPipeline)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(transitionAnalysisRunner);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(reportDataFactory);
        ArgumentNullException.ThrowIfNull(reportPipeline);

        _settings = settings;
        _transitionAnalysisRunner = transitionAnalysisRunner;
        _presentation = presentation;
        _reportDataFactory = reportDataFactory;
        _reportPipeline = reportPipeline;
    }

    public async Task<ReportGenerationOutcome> RunAsync(
        JiraApplicationReportData reportData,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var reportContext = reportData.ReportContext;
        _presentation.ShowReportHeader(_settings, reportContext.TransitionIssueCount);
        var analysisResult = await _transitionAnalysisRunner
            .RunAsync(reportContext, cancellationToken)
            .ConfigureAwait(false);

        return analysisResult switch
        {
            JiraTransitionAnalysisResult.NoTransitionIssues => CompleteWithoutAnalysis(
                reportData,
                analysisResult,
                _presentation.ShowNoIssuesMatchedFilter),
            JiraTransitionAnalysisResult.NoIssuesLoaded noIssuesLoaded => CompleteWithoutAnalysis(
                reportData,
                analysisResult,
                _presentation.ShowNoIssuesLoaded,
                noIssuesLoaded.LoadResult.Failures),
            JiraTransitionAnalysisResult.NoIssuesMatchedTypeFilter noTypeMatch =>
                CompleteWithoutAnalysis(
                    reportData,
                    analysisResult,
                    _presentation.ShowNoIssuesMatchedFilter,
                    noTypeMatch.LoadResult.Failures),
            JiraTransitionAnalysisResult.NoIssuesMatchedRequiredStage noStageMatch =>
                CompleteWithoutAnalysis(
                    reportData,
                    analysisResult,
                    _presentation.ShowNoIssuesMatchedRequiredStage,
                    noStageMatch.LoadResult.Failures),
            JiraTransitionAnalysisResult.Success success => CompleteSuccessfulAnalysis(
                reportData,
                success),
            _ => throw new InvalidOperationException(
                $"Unsupported transition-analysis result: {analysisResult.GetType().Name}.")
        };
    }

    private ReportGenerationOutcome CompleteWithoutAnalysis(
        JiraApplicationReportData reportData,
        JiraTransitionAnalysisResult analysisResult,
        Action showOutcome,
        IReadOnlyList<LoadFailure>? failures = null)
    {
        showOutcome();
        if (failures is { Count: > 0 })
        {
            _presentation.ShowFailures(failures);
        }

        ShowOpenIssuesSummary(reportData.ReportContext);
        return RenderReport(reportData, analysisResult);
    }

    private ReportGenerationOutcome CompleteSuccessfulAnalysis(
        JiraApplicationReportData reportData,
        JiraTransitionAnalysisResult.Success success)
    {
        PresentSuccessfulAnalysis(success.Analysis);
        ShowOpenIssuesSummary(reportData.ReportContext);
        var reportGenerationOutcome = RenderReport(reportData, success);

        if (success.LoadResult.Failures.Count > 0)
        {
            _presentation.ShowSpacer();
            _presentation.ShowFailures(success.LoadResult.Failures);
        }

        return reportGenerationOutcome;
    }

    private void PresentSuccessfulAnalysis(SuccessfulJiraIssueAnalysis analysis)
    {
        _presentation.ShowProcessingStep(
            "Calculating transition metrics and percentiles...");
        _presentation.ShowDoneIssuesTable(analysis.DoneIssues, _settings.DoneStatusName);
        _presentation.ShowSpacer();
        _presentation.ShowDoneDaysAtWork75PerType(
            analysis.DoneDaysAtWork75PerType,
            _settings.DoneStatusName);
        _presentation.ShowSpacer();

        if (_settings.RejectStatusName is { } rejectStatusName)
        {
            _presentation.ShowRejectedIssuesTable(analysis.RejectedIssues, rejectStatusName);
            _presentation.ShowSpacer();
        }

        _presentation.ShowProcessingStep("Building path groups...");
        _presentation.ShowPathGroupsSummary(analysis.PathSummary);
        _presentation.ShowSpacer();
        _presentation.ShowPathGroups(analysis.PathGroups);
    }

    private ReportGenerationOutcome RenderReport(
        JiraApplicationReportData reportData,
        JiraTransitionAnalysisResult analysisResult)
    {
        _presentation.ShowProcessingStep("Rendering reports...");
        return _reportPipeline.RenderReport(
            _reportDataFactory.Create(reportData, analysisResult));
    }

    private void ShowOpenIssuesSummary(JiraReportContext reportContext)
    {
        if (!_settings.ShowGeneralStatistics)
        {
            return;
        }

        _presentation.ShowOpenIssuesByStatusSummary(
            reportContext.OpenIssuesByStatus,
            _settings.DoneStatusName,
            _settings.RejectStatusName);
        _presentation.ShowSpacer();
    }

    private readonly AppSettings _settings;
    private readonly JiraTransitionAnalysisRunner _transitionAnalysisRunner;
    private readonly IJiraPresentationService _presentation;
    private readonly JiraReportDataFactory _reportDataFactory;
    private readonly IJiraReportPipeline _reportPipeline;
}
