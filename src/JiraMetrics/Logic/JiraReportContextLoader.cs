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
        var unresolved30DaysTasksTask = LoadUnresolved30DaysTasksAsync(settings, cancellationToken);
        var openIssuesByStatusTask = LoadOpenIssuesByStatusAsync(settings, cancellationToken);
        var roadmapItemsTask = LoadRoadmapItemsAsync(settings, cancellationToken);

        var pendingLoads = new List<Task>
        {
            issueKeysTask,
            rejectIssueKeysTask
        };
        AddPendingLoad(pendingLoads, releaseIssuesTask);
        AddPendingLoad(pendingLoads, archTasksTask);
        AddPendingLoad(pendingLoads, globalIncidentsTask);
        AddPendingLoad(pendingLoads, unresolved30DaysTasksTask);
        AddPendingLoad(pendingLoads, openIssuesByStatusTask);
        AddPendingLoad(pendingLoads, roadmapItemsTask);

        await Task.WhenAll(pendingLoads).ConfigureAwait(false);

        var optionalSectionFailures = new List<OptionalSectionLoadFailure>();

        return new JiraReportContext(
            await issueKeysTask.ConfigureAwait(false),
            await rejectIssueKeysTask.ConfigureAwait(false),
            await AwaitOptionalAsync(releaseIssuesTask, optionalSectionFailures).ConfigureAwait(false),
            await AwaitOptionalAsync(archTasksTask, optionalSectionFailures).ConfigureAwait(false),
            await AwaitOptionalAsync(globalIncidentsTask, optionalSectionFailures).ConfigureAwait(false),
            await AwaitOptionalAsync(unresolved30DaysTasksTask, optionalSectionFailures).ConfigureAwait(false),
            await AwaitOptionalAsync(openIssuesByStatusTask, optionalSectionFailures).ConfigureAwait(false),
            await AwaitOptionalAsync(roadmapItemsTask, optionalSectionFailures).ConfigureAwait(false))
        {
            OptionalSectionFailures = optionalSectionFailures
        };
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

    private Task<OptionalSectionLoadResult<IReadOnlyList<ReleaseIssueItem>>>? LoadReleaseIssuesAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ReleaseReport is not { } releaseReport)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.ReleaseReport,
            token => _reportDataClient.GetReleaseIssuesForMonthAsync(
                BuildReleaseIssueReadRequest(releaseReport),
                token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IReadOnlyList<ArchTaskItem>>>? LoadArchTasksAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ArchTasksReport is not { } archTasksReport)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.ArchTasksReport,
            token => _reportDataClient.GetArchTasksAsync(archTasksReport, token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IReadOnlyList<GlobalIncidentItem>>>? LoadGlobalIncidentsAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.GlobalIncidentsReport is not { } globalIncidentsReport)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.GlobalIncidentsReport,
            token => _reportDataClient.GetGlobalIncidentsForMonthAsync(globalIncidentsReport, token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IReadOnlyList<IssueListItem>>>? LoadUnresolved30DaysTasksAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.Unresolved30DaysTasksReport is not { } unresolvedTasksReport)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.Unresolved30DaysTasksReport,
            token => _reportDataClient.GetUnresolved30DaysTasksAsync(unresolvedTasksReport, token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IReadOnlyList<StatusIssueTypeSummary>>>? LoadOpenIssuesByStatusAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.ShowGeneralStatistics)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.GeneralStatistics,
            token => _issueSearchClient.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
                settings.ProjectKey,
                settings.DoneStatusName,
                settings.RejectStatusName,
                token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IReadOnlyList<RoadmapItem>>>? LoadRoadmapItemsAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.RoadmapReport is not { } roadmapReport)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.RoadmapReport,
            token => _reportDataClient.GetRoadmapItemsAsync(roadmapReport, token),
            cancellationToken);
    }

    private static void AddPendingLoad(List<Task> pendingLoads, Task? pendingLoad)
    {
        if (pendingLoad is not null)
        {
            pendingLoads.Add(pendingLoad);
        }
    }

    private static async Task<IReadOnlyList<T>> AwaitOptionalAsync<T>(
        Task<OptionalSectionLoadResult<IReadOnlyList<T>>>? task,
        List<OptionalSectionLoadFailure> failures)
    {
        if (task is null)
        {
            return [];
        }

        var result = await task.ConfigureAwait(false);
        if (result is OptionalSectionLoadResult<IReadOnlyList<T>>.Loaded loaded)
        {
            return loaded.Value;
        }

        failures.Add(
            ((OptionalSectionLoadResult<IReadOnlyList<T>>.Failed)result).Failure);
        return [];
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

