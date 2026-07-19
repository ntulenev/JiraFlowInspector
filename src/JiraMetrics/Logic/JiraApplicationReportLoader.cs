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
        IJiraStatusPresenter statusPresenter,
        IJiraReportSectionsPresenter reportSectionsPresenter)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(statusPresenter);
        ArgumentNullException.ThrowIfNull(reportSectionsPresenter);

        _settings = settings;
        _dataFacade = dataFacade;
        _statusPresenter = statusPresenter;
        _reportSectionsPresenter = reportSectionsPresenter;
    }

    public Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken) =>
        _dataFacade.GetCurrentUserAsync(cancellationToken);

    public async Task<ReportLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        _statusPresenter.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _statusPresenter.ShowSpacer();

        ShowOptionalReportLoadingStarted();
        _reportSectionsPresenter.ShowAllTasksRatioLoadingStarted();

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
                return ReportLoadResult.Failure.Instance;
            }

            ShowOptionalReports(reportContext);

            var allTasksRatio = await LoadAndShowAllTasksRatioAsync(allTasksRatioTask).ConfigureAwait(false);
            if (allTasksRatio is null)
            {
                return ReportLoadResult.Failure.Instance;
            }

            var bugRatio = await LoadAndShowBugRatioAsync(bugRatioTask).ConfigureAwait(false);
            if (bugRatioTask is not null && bugRatio is null)
            {
                return ReportLoadResult.Failure.Instance;
            }

            var internalIncidents = await LoadInternalIncidentsAsync(internalIncidentsTask).ConfigureAwait(false);
            if (internalIncidentsTask is not null && internalIncidents is null)
            {
                return ReportLoadResult.Failure.Instance;
            }

            var testCoverage = await LoadAndShowTestCoverageAsync(testCoverageTask).ConfigureAwait(false);
            if (testCoverageTask is not null && testCoverage is null)
            {
                return ReportLoadResult.Failure.Instance;
            }

            return new ReportLoadResult.Success(
                new JiraApplicationReportData(
                    reportContext,
                    allTasksRatio,
                    bugRatio,
                    internalIncidents,
                    testCoverage ?? TestCoverageSnapshot.Empty));
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
            _reportSectionsPresenter.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportSectionsPresenter.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _statusPresenter.ShowSpacer();
            _reportSectionsPresenter.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _statusPresenter.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _reportSectionsPresenter.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _statusPresenter.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _statusPresenter.ShowSpacer();
        }
    }

    private Task<IssueRatioSnapshot?>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        _reportSectionsPresenter.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
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

        _reportSectionsPresenter.ShowTestCoverageLoadingStarted(testCoverageSettings);
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

        _reportSectionsPresenter.ShowAllTasksRatioLoadingCompleted(allTasksRatio);
        _reportSectionsPresenter.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            allTasksRatio);
        _statusPresenter.ShowSpacer();

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

        _reportSectionsPresenter.ShowBugRatioLoadingCompleted(bugRatio);
        _reportSectionsPresenter.ShowBugRatio(
            _settings.BugIssueNames,
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            bugRatio);
        _statusPresenter.ShowSpacer();

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

        _reportSectionsPresenter.ShowTestCoverage(testCoverageSettings, testCoverage);
        _statusPresenter.ShowSpacer();

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
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _statusPresenter.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraStatusPresenter _statusPresenter;
    private readonly IJiraReportSectionsPresenter _reportSectionsPresenter;
}
