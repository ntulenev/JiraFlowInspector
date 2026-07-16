using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads report context and auxiliary ratio data used before detailed analysis starts.
/// </summary>
internal sealed class JiraApplicationReportLoader : IJiraApplicationReportLoader
{
    public JiraApplicationReportLoader(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationReportingFacade reportingFacade)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(reportingFacade);

        _settings = settings;
        _dataFacade = dataFacade;
        _reportingFacade = reportingFacade;
    }

    public Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken) =>
        _dataFacade.GetCurrentUserAsync(cancellationToken);

    public async Task<JiraApplicationReportData?> TryLoadAsync(CancellationToken cancellationToken)
    {
        _reportingFacade.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _reportingFacade.ShowSpacer();

        ShowOptionalReportLoadingStarted();
        _reportingFacade.ShowAllTasksRatioLoadingStarted();

        using var pendingLoadsCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pendingLoadsToken = pendingLoadsCancellation.Token;

        var reportContextTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadReportContextAsync(_settings, pendingLoadsToken));
        var allTasksRatioTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(_settings, [], pendingLoadsToken));
        var bugRatioTask = StartBugRatioLoading(pendingLoadsToken);
        var internalIncidentsTask = StartInternalIncidentsLoading(pendingLoadsToken);
        var testCoverageTask = StartTestCoverageLoading(pendingLoadsToken);

        var pendingLoads = new List<Task>
        {
            reportContextTask,
            allTasksRatioTask
        };
        AddPendingLoad(pendingLoads, bugRatioTask);
        AddPendingLoad(pendingLoads, internalIncidentsTask);
        AddPendingLoad(pendingLoads, testCoverageTask);

        try
        {
            var reportContext = await reportContextTask.ConfigureAwait(false);
            if (reportContext is null)
            {
                return null;
            }

            ShowOptionalReports(reportContext);

            var allTasksRatio = await LoadAndShowAllTasksRatioAsync(allTasksRatioTask).ConfigureAwait(false);
            if (allTasksRatio is null)
            {
                return null;
            }

            var bugRatio = await LoadAndShowBugRatioAsync(bugRatioTask).ConfigureAwait(false);
            if (bugRatioTask is not null && bugRatio is null)
            {
                return null;
            }

            var internalIncidents = await LoadInternalIncidentsAsync(internalIncidentsTask).ConfigureAwait(false);
            if (internalIncidentsTask is not null && internalIncidents is null)
            {
                return null;
            }

            var testCoverage = await LoadAndShowTestCoverageAsync(testCoverageTask).ConfigureAwait(false);
            if (testCoverageTask is not null && testCoverage is null)
            {
                return null;
            }

            return new JiraApplicationReportData(
                reportContext,
                allTasksRatio,
                bugRatio,
                internalIncidents,
                testCoverage ?? TestCoverageSnapshot.Empty);
        }
        finally
        {
            await pendingLoadsCancellation.CancelAsync().ConfigureAwait(false);
            try
            {
                await Task.WhenAll(pendingLoads).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private static void AddPendingLoad(List<Task> pendingLoads, Task? pendingLoad)
    {
        if (pendingLoad is not null)
        {
            pendingLoads.Add(pendingLoad);
        }
    }

    private void ShowOptionalReportLoadingStarted()
    {
        if (_settings.ReleaseReport is not null)
        {
            _reportingFacade.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportingFacade.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportingFacade.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _reportingFacade.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _reportingFacade.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _reportingFacade.ShowSpacer();
        }
    }

    private Task<IssueRatioSnapshot?>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.BugIssueNames,
                cancellationToken));
    }

    private Task<IssueRatioSnapshot?>? StartInternalIncidentsLoading(CancellationToken cancellationToken)
    {
        if (_settings.InternalIncidentIssueNames.Count == 0)
        {
            return null;
        }

        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.InternalIncidentIssueNames,
                cancellationToken));
    }

    private Task<TestCoverageSnapshot?>? StartTestCoverageLoading(CancellationToken cancellationToken)
    {
        if (_settings.TestCoverage is not { Enabled: true } testCoverageSettings)
        {
            return null;
        }

        _reportingFacade.ShowTestCoverageLoadingStarted(testCoverageSettings);
        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadTestCoverageAsync(_settings, testCoverageSettings, cancellationToken));
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowAllTasksRatioAsync(Task<IssueRatioSnapshot?> allTasksRatioTask)
    {
        var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowAllTasksRatioLoadingCompleted(allTasksRatio);
        _reportingFacade.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            allTasksRatio);
        _reportingFacade.ShowSpacer();

        return allTasksRatio;
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowBugRatioAsync(Task<IssueRatioSnapshot?>? bugRatioTask)
    {
        if (bugRatioTask is null)
        {
            return null;
        }

        var bugRatio = await bugRatioTask.ConfigureAwait(false);
        if (bugRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingCompleted(bugRatio);
        _reportingFacade.ShowBugRatio(
            _settings.BugIssueNames,
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            bugRatio);
        _reportingFacade.ShowSpacer();

        return bugRatio;
    }

    private static async Task<IssueRatioSnapshot?> LoadInternalIncidentsAsync(
        Task<IssueRatioSnapshot?>? internalIncidentsTask)
    {
        if (internalIncidentsTask is null)
        {
            return null;
        }

        return await internalIncidentsTask.ConfigureAwait(false);
    }

    private async Task<TestCoverageSnapshot?> LoadAndShowTestCoverageAsync(
        Task<TestCoverageSnapshot?>? testCoverageTask)
    {
        if (testCoverageTask is null || _settings.TestCoverage is not { } testCoverageSettings)
        {
            return null;
        }

        var testCoverage = await testCoverageTask.ConfigureAwait(false);
        if (testCoverage is null)
        {
            return null;
        }

        _reportingFacade.ShowTestCoverage(testCoverageSettings, testCoverage);
        _reportingFacade.ShowSpacer();

        return testCoverage;
    }

    private async Task<T?> TryLoadSearchDataAsync<T>(Func<Task<T>> loadAsync)
        where T : class
    {
        try
        {
            return await loadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationReportingFacade _reportingFacade;
}
