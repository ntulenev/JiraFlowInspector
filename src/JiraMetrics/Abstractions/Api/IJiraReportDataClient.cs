using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Abstractions.Api;

internal interface IJiraReportDataClient
{
    Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ReleaseIssueReadRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken);
}

