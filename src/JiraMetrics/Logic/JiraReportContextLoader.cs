using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads pre-analysis Jira data for the main application workflow.
/// </summary>
internal sealed class JiraReportContextLoader
{

    public JiraReportContextLoader(
        IJiraIssueSearchClient issueSearchClient,
        IJiraReportDataClient reportDataClient)
    {
        ArgumentNullException.ThrowIfNull(issueSearchClient);
        ArgumentNullException.ThrowIfNull(reportDataClient);
        _issueSearchClient = issueSearchClient;
        _reportDataClient = reportDataClient;
    }

    public async Task<JiraReportContext> LoadAsync(
        AppSettings settings,
        CancellationToken cancellationToken,
        IssueSearchSnapshot? allTasksSnapshot = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var issueKeysTask = LoadIssueKeysAsync(
            settings,
            settings.DoneStatusName,
            allTasksSnapshot,
            snapshotSelector: static snapshot => snapshot.DoneIssues.Select(static issue => issue.Key),
            cancellationToken);
        var rejectIssueKeysTask = LoadRejectIssueKeysAsync(settings, allTasksSnapshot, cancellationToken);
        var releaseIssuesTask = LoadReleaseIssuesAsync(settings, cancellationToken);
        var archTasksTask = LoadArchTasksAsync(settings, cancellationToken);
        var globalIncidentsTask = LoadGlobalIncidentsAsync(settings, cancellationToken);
        var openIssuesByStatusTask = LoadOpenIssuesByStatusAsync(settings, cancellationToken);

        await Task.WhenAll(
                issueKeysTask,
                rejectIssueKeysTask,
                releaseIssuesTask,
                archTasksTask,
                globalIncidentsTask,
                openIssuesByStatusTask)
            .ConfigureAwait(false);

        return new JiraReportContext(
            await issueKeysTask.ConfigureAwait(false),
            await rejectIssueKeysTask.ConfigureAwait(false),
            await releaseIssuesTask.ConfigureAwait(false),
            await archTasksTask.ConfigureAwait(false),
            await globalIncidentsTask.ConfigureAwait(false),
            await openIssuesByStatusTask.ConfigureAwait(false));
    }

    private Task<IReadOnlyList<IssueKey>> LoadIssueKeysAsync(
        AppSettings settings,
        StatusName statusName,
        IssueSearchSnapshot? allTasksSnapshot,
        Func<IssueSearchSnapshot, IEnumerable<IssueKey>> snapshotSelector,
        CancellationToken cancellationToken)
    {
        if (settings.CreatedAfter is null && allTasksSnapshot is not null)
        {
            return Task.FromResult<IReadOnlyList<IssueKey>>([.. snapshotSelector(allTasksSnapshot)]);
        }

        return _issueSearchClient.GetIssueKeysMovedToDoneThisMonthAsync(
            settings.ProjectKey,
            statusName,
            settings.CreatedAfter,
            cancellationToken);
    }

    private Task<IReadOnlyList<IssueKey>> LoadRejectIssueKeysAsync(
        AppSettings settings,
        IssueSearchSnapshot? allTasksSnapshot,
        CancellationToken cancellationToken)
    {
        if (settings.RejectStatusName is not { } rejectStatusName)
        {
            return Task.FromResult<IReadOnlyList<IssueKey>>([]);
        }

        return LoadIssueKeysAsync(
            settings,
            rejectStatusName,
            allTasksSnapshot,
            static snapshot => snapshot.RejectedIssues.Select(static issue => issue.Key),
            cancellationToken);
    }

    private Task<IReadOnlyList<ReleaseIssueItem>> LoadReleaseIssuesAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ReleaseReport is not { } releaseReport)
        {
            return Task.FromResult<IReadOnlyList<ReleaseIssueItem>>([]);
        }

        return _reportDataClient.GetReleaseIssuesForMonthAsync(
            BuildReleaseIssueReadRequest(releaseReport),
            cancellationToken);
    }

    private Task<IReadOnlyList<ArchTaskItem>> LoadArchTasksAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ArchTasksReport is not { } archTasksReport)
        {
            return Task.FromResult<IReadOnlyList<ArchTaskItem>>([]);
        }

        return _reportDataClient.GetArchTasksAsync(archTasksReport, cancellationToken);
    }

    private Task<IReadOnlyList<GlobalIncidentItem>> LoadGlobalIncidentsAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.GlobalIncidentsReport is not { } globalIncidentsReport)
        {
            return Task.FromResult<IReadOnlyList<GlobalIncidentItem>>([]);
        }

        return _reportDataClient.GetGlobalIncidentsForMonthAsync(globalIncidentsReport, cancellationToken);
    }

    private Task<IReadOnlyList<StatusIssueTypeSummary>> LoadOpenIssuesByStatusAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.ShowGeneralStatistics)
        {
            return Task.FromResult<IReadOnlyList<StatusIssueTypeSummary>>([]);
        }

        return _issueSearchClient.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            settings.ProjectKey,
            settings.DoneStatusName,
            settings.RejectStatusName,
            cancellationToken);
    }
    private readonly IJiraIssueSearchClient _issueSearchClient;
    private readonly IJiraReportDataClient _reportDataClient;

    private static ReleaseIssueReadRequest BuildReleaseIssueReadRequest(ReleaseReportSettings releaseReport)
    {
        ArgumentNullException.ThrowIfNull(releaseReport);

        var hotFixRules = releaseReport.HotFixRules
            .Select(static pair => new HotFixRule(
                new JiraFieldName(pair.Key),
                [.. pair.Value.Select(static value => new JiraFieldValue(value))]))
            .ToArray();
        var environmentFilter = JiraFieldName.FromNullable(releaseReport.EnvironmentFieldName) is { } environmentFieldName
            && JiraFieldValue.FromNullable(releaseReport.EnvironmentFieldValue) is { } environmentFieldValue
                ? new ReleaseEnvironmentFilter(environmentFieldName, environmentFieldValue)
                : null;

        return new ReleaseIssueReadRequest(
            releaseReport.ReleaseProjectKey,
            new JiraLabel(releaseReport.ProjectLabel),
            new JiraFieldName(releaseReport.ReleaseDateFieldName),
            JiraFieldName.FromNullable(releaseReport.ComponentsFieldName),
            hotFixRules,
            new JiraFieldName(releaseReport.RollbackFieldName),
            environmentFilter);
    }
}

