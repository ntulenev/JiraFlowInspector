using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Application.Workflow;

/// <summary>
/// Loads report data required before detailed issue analysis starts.
/// </summary>
internal interface IJiraApplicationReportLoader
{
    Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken);

    Task<JiraApplicationReportData?> TryLoadAsync(CancellationToken cancellationToken);
}
