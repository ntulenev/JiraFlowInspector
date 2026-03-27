using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

internal interface IJiraReportDataClient
{
    Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken);
}
