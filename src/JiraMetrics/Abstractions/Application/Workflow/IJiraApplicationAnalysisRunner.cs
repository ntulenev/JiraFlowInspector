using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Application.Workflow;

/// <summary>
/// Runs timeline loading, analysis, presentation, and rendering for prepared report data.
/// </summary>
internal interface IJiraApplicationAnalysisRunner
{
    Task RunAsync(JiraApplicationReportData reportData, CancellationToken cancellationToken);
}
