using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads report context and auxiliary data without performing presentation work.
/// </summary>
internal sealed class JiraApplicationReportLoader : IJiraApplicationReportLoader
{
    public JiraApplicationReportLoader(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);

        _settings = settings;
        _dataFacade = dataFacade;
    }

    public Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken) =>
        _dataFacade.GetCurrentUserAsync(cancellationToken);

    public async Task<ReportLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        using var pendingLoadsCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pendingLoadsToken = pendingLoadsCancellation.Token;

        var reportContextTask = _dataFacade.LoadReportContextAsync(_settings, pendingLoadsToken);
        var allTasksRatioTask = _dataFacade.LoadIssueRatioAsync(_settings, [], pendingLoadsToken);
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
            var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
            var bugRatio = await AwaitOptionalAsync(bugRatioTask).ConfigureAwait(false);
            var internalIncidents = await AwaitOptionalAsync(internalIncidentsTask).ConfigureAwait(false);
            var testCoverage = await AwaitOptionalAsync(testCoverageTask).ConfigureAwait(false)
                ?? TestCoverageSnapshot.Empty;

            return new ReportLoadResult.Success(
                new JiraApplicationReportData(
                    reportContext,
                    allTasksRatio,
                    bugRatio,
                    internalIncidents,
                    testCoverage));
        }
        catch (Exception ex) when (IsExpectedLoadFailure(ex))
        {
            return new ReportLoadResult.Failure(ErrorMessage.FromException(ex));
        }
        finally
        {
            await pendingLoadsCancellation.CancelAsync().ConfigureAwait(false);
            await ObservePendingLoadsAsync(pendingLoads, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void AddPendingLoad(List<Task> pendingLoads, Task? pendingLoad)
    {
        if (pendingLoad is not null)
        {
            pendingLoads.Add(pendingLoad);
        }
    }

    private Task<IssueRatioSnapshot>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        return _dataFacade.LoadIssueRatioAsync(
            _settings,
            _settings.BugIssueNames,
            cancellationToken);
    }

    private Task<IssueRatioSnapshot>? StartInternalIncidentsLoading(CancellationToken cancellationToken)
    {
        if (_settings.InternalIncidentIssueNames.Count == 0)
        {
            return null;
        }

        return _dataFacade.LoadIssueRatioAsync(
            _settings,
            _settings.InternalIncidentIssueNames,
            cancellationToken);
    }

    private Task<TestCoverageSnapshot>? StartTestCoverageLoading(CancellationToken cancellationToken)
    {
        if (_settings.TestCoverage is not { Enabled: true } testCoverageSettings)
        {
            return null;
        }

        return _dataFacade.LoadTestCoverageAsync(_settings, testCoverageSettings, cancellationToken);
    }

    private static async Task<T?> AwaitOptionalAsync<T>(Task<T>? task)
        where T : class
    {
        if (task is null)
        {
            return null;
        }

        return await task.ConfigureAwait(false);
    }

    private static async Task ObservePendingLoadsAsync(
        IReadOnlyList<Task> pendingLoads,
        CancellationToken callerCancellationToken)
    {
        try
        {
            await Task.WhenAll(pendingLoads).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!callerCancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            // The primary load result already carries the failure that stopped the workflow.
        }
        catch (InvalidOperationException)
        {
            // The primary load result already carries the failure that stopped the workflow.
        }
        catch (JsonException)
        {
            // The primary load result already carries the failure that stopped the workflow.
        }
    }

    private static bool IsExpectedLoadFailure(Exception exception) =>
        exception is HttpRequestException or InvalidOperationException or JsonException;

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
}
