using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Builds renderer input from application and transition-analysis results.
/// </summary>
internal sealed class JiraReportDataFactory
{
    public JiraReportDataFactory(AppSettings settings, ReportRunContext runContext)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runContext);

        _settings = settings;
        _runContext = runContext;
    }

    public JiraReportData Create(
        JiraApplicationReportData reportData,
        JiraTransitionAnalysisResult analysisResult)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(analysisResult);

        return analysisResult switch
        {
            JiraTransitionAnalysisResult.Success success => JiraReportData.Create(
                _runContext,
                _settings,
                reportData.ReportContext,
                reportData.AllTasksRatio,
                reportData.BugRatio,
                reportData.InternalIncidents,
                reportData.TestCoverage,
                success.Analysis,
                success.LoadResult.Failures),
            JiraTransitionAnalysisResult.NoTransitionIssues => CreateWithoutAnalysis(
                reportData,
                failures: [],
                successfulCount: new ItemCount(0)),
            JiraTransitionAnalysisResult.NoIssuesLoaded noIssuesLoaded => CreateWithoutAnalysis(
                reportData,
                noIssuesLoaded.LoadResult.Failures,
                noIssuesLoaded.LoadResult.LoadedIssueCount),
            JiraTransitionAnalysisResult.NoIssuesMatchedTypeFilter noTypeMatch =>
                CreateWithoutAnalysis(
                    reportData,
                    noTypeMatch.LoadResult.Failures,
                    noTypeMatch.LoadResult.LoadedIssueCount),
            JiraTransitionAnalysisResult.NoIssuesMatchedRequiredStage noStageMatch =>
                CreateWithoutAnalysis(
                    reportData,
                    noStageMatch.LoadResult.Failures,
                    noStageMatch.LoadResult.LoadedIssueCount),
            _ => throw new InvalidOperationException(
                $"Unsupported transition-analysis result: {analysisResult.GetType().Name}.")
        };
    }

    private JiraReportData CreateWithoutAnalysis(
        JiraApplicationReportData reportData,
        IReadOnlyList<LoadFailure> failures,
        ItemCount successfulCount) =>
        JiraReportData.CreateWithoutTransitionAnalysis(
            _runContext,
            _settings,
            reportData.ReportContext,
            reportData.AllTasksRatio,
            reportData.BugRatio,
            reportData.InternalIncidents,
            reportData.TestCoverage,
            failures,
            successfulCount,
            matchedStageCount: new ItemCount(0));

    private readonly AppSettings _settings;
    private readonly ReportRunContext _runContext;
}
