using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads pre-analysis Jira data for the main application workflow.
/// </summary>
internal sealed class JiraReportContextLoader
{
    private readonly IJiraApiClient _apiClient;

    public JiraReportContextLoader(IJiraApiClient apiClient)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        _apiClient = apiClient;
    }

    public async Task<JiraReportContext> LoadAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var issueKeys = await _apiClient.GetIssueKeysMovedToDoneThisMonthAsync(
            settings.ProjectKey,
            settings.DoneStatusName,
            settings.CreatedAfter,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<IssueKey> rejectIssueKeys = [];
        if (settings.RejectStatusName is { } rejectStatusName)
        {
            rejectIssueKeys = await _apiClient.GetIssueKeysMovedToDoneThisMonthAsync(
                settings.ProjectKey,
                rejectStatusName,
                settings.CreatedAfter,
                cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<ReleaseIssueItem> releaseIssues = [];
        if (settings.ReleaseReport is { } releaseReport)
        {
            releaseIssues = await _apiClient.GetReleaseIssuesForMonthAsync(
                releaseReport.ReleaseProjectKey,
                releaseReport.ProjectLabel,
                releaseReport.ReleaseDateFieldName,
                releaseReport.ComponentsFieldName,
                releaseReport.HotFixRules,
                releaseReport.RollbackFieldName,
                releaseReport.EnvironmentFieldName,
                releaseReport.EnvironmentFieldValue,
                cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<GlobalIncidentItem> globalIncidents = [];
        if (settings.GlobalIncidentsReport is { } globalIncidentsReport)
        {
            globalIncidents = await _apiClient
                .GetGlobalIncidentsForMonthAsync(globalIncidentsReport, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<StatusIssueTypeSummary> openIssuesByStatus = [];
        if (settings.ShowGeneralStatistics)
        {
            openIssuesByStatus = await _apiClient.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
                settings.ProjectKey,
                settings.DoneStatusName,
                settings.RejectStatusName,
                cancellationToken).ConfigureAwait(false);
        }

        return new JiraReportContext(
            issueKeys,
            rejectIssueKeys,
            releaseIssues,
            globalIncidents,
            openIssuesByStatus);
    }
}
