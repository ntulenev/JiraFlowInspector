using JiraMetrics.Models;

namespace JiraMetrics.Logic;

/// <summary>
/// Runs timeline loading, analysis, presentation, and rendering for prepared report data.
/// </summary>
internal interface IJiraApplicationAnalysisRunner
{
    Task RunAsync(JiraApplicationReportData reportData, CancellationToken cancellationToken);
}
