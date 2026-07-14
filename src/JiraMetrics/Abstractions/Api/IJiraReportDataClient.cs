using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Abstractions.Api;

/// <summary>
/// Loads Jira datasets used by optional report sections.
/// </summary>
internal interface IJiraReportDataClient
{
    Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ReleaseIssueReadRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IssueListItem>> GetUnresolved30DaysTasksAsync(
        Unresolved30DaysTasksReportSettings settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoadmapItem>> GetRoadmapItemsAsync(
        RoadmapReportSettings settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken);
}

