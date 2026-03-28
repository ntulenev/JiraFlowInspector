using JiraMetrics.Models;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads report data required before detailed issue analysis starts.
/// </summary>
internal interface IJiraApplicationReportLoader
{
    Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken);

    Task<JiraApplicationReportData?> TryLoadAsync(CancellationToken cancellationToken);
}
