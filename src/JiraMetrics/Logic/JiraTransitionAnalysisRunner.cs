using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads issue timelines and applies transition analysis without rendering reports.
/// </summary>
internal sealed class JiraTransitionAnalysisRunner
{
    public JiraTransitionAnalysisRunner(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationAnalysisFacade analysisFacade,
        IJiraStatusPresenter statusPresenter)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(statusPresenter);

        _settings = settings;
        _dataFacade = dataFacade;
        _analysisFacade = analysisFacade;
        _statusPresenter = statusPresenter;
    }

    public async Task<JiraTransitionAnalysisResult> RunAsync(
        JiraReportContext reportContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reportContext);

        if (reportContext.TransitionIssueCount.Value == 0)
        {
            return new JiraTransitionAnalysisResult.NoTransitionIssues();
        }

        var loadResult = await _dataFacade.LoadIssueTimelinesAsync(
            reportContext.IssueKeys,
            reportContext.RejectIssueKeys,
            cancellationToken).ConfigureAwait(false);
        if (loadResult.DoneIssues.Count == 0 && loadResult.RejectIssues.Count == 0)
        {
            return new JiraTransitionAnalysisResult.NoIssuesLoaded(loadResult);
        }

        _statusPresenter.ShowProcessingStep(
            "Applying issue type and required-stage filters...");
        var analysis = _analysisFacade.Analyze(
            loadResult.DoneIssues,
            loadResult.RejectIssues,
            loadResult.Failures,
            _settings);

        return analysis switch
        {
            SuccessfulJiraIssueAnalysis success =>
                new JiraTransitionAnalysisResult.Success(loadResult, success),
            NoIssuesMatchedTypeFilterAnalysis =>
                new JiraTransitionAnalysisResult.NoIssuesMatchedTypeFilter(loadResult),
            NoIssuesMatchedRequiredStageAnalysis =>
                new JiraTransitionAnalysisResult.NoIssuesMatchedRequiredStage(loadResult),
            _ => throw new InvalidOperationException(
                $"Unsupported issue-analysis result: {analysis.GetType().Name}.")
        };
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationAnalysisFacade _analysisFacade;
    private readonly IJiraStatusPresenter _statusPresenter;
}
