using System.Runtime.ExceptionServices;

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
            var optionalSectionFailures = reportContext.OptionalSectionFailures.ToList();
            var bugRatio = await AwaitOptionalAsync(
                bugRatioTask,
                optionalSectionFailures).ConfigureAwait(false);
            var internalIncidents = await AwaitOptionalAsync(
                internalIncidentsTask,
                optionalSectionFailures).ConfigureAwait(false);
            var testCoverage = await AwaitOptionalAsync(
                testCoverageTask,
                optionalSectionFailures).ConfigureAwait(false)
                ?? TestCoverageSnapshot.Empty;

            return new ReportLoadResult.Success(
                new JiraApplicationReportData(
                    reportContext,
                    allTasksRatio,
                    bugRatio,
                    internalIncidents,
                    testCoverage)
                {
                    OptionalSectionFailures = optionalSectionFailures
                });
        }
        catch (Exception ex) when (ReportLoadExceptionClassifier.IsExpected(ex))
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

    private Task<OptionalSectionLoadResult<IssueRatioSnapshot>>? StartBugRatioLoading(
        CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.BugRatio,
            token => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.BugIssueNames,
                token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<IssueRatioSnapshot>>? StartInternalIncidentsLoading(
        CancellationToken cancellationToken)
    {
        if (_settings.InternalIncidentIssueNames.Count == 0)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.InternalIncidents,
            token => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.InternalIncidentIssueNames,
                token),
            cancellationToken);
    }

    private Task<OptionalSectionLoadResult<TestCoverageSnapshot>>? StartTestCoverageLoading(
        CancellationToken cancellationToken)
    {
        if (_settings.TestCoverage is not { Enabled: true } testCoverageSettings)
        {
            return null;
        }

        return OptionalSectionLoader.LoadAsync(
            OptionalReportSection.TestCoverage,
            token => _dataFacade.LoadTestCoverageAsync(_settings, testCoverageSettings, token),
            cancellationToken);
    }

    private static async Task<T?> AwaitOptionalAsync<T>(
        Task<OptionalSectionLoadResult<T>>? task,
        List<OptionalSectionLoadFailure> failures)
        where T : class
    {
        if (task is null)
        {
            return null;
        }

        var result = await task.ConfigureAwait(false);
        if (result is OptionalSectionLoadResult<T>.Loaded loaded)
        {
            return loaded.Value;
        }

        failures.Add(((OptionalSectionLoadResult<T>.Failed)result).Failure);
        return null;
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
        catch (Exception ex) when (ReportLoadExceptionClassifier.IsExpected(ex))
        {
            ThrowUnexpectedFailure(pendingLoads);
        }
    }

    private static void ThrowUnexpectedFailure(IReadOnlyList<Task> pendingLoads)
    {
        var unexpectedFailure = pendingLoads
            .Where(static task => task.IsFaulted)
            .SelectMany(static task => task.Exception!.Flatten().InnerExceptions)
            .FirstOrDefault(static exception => !ReportLoadExceptionClassifier.IsExpected(exception));
        if (unexpectedFailure is not null)
        {
            ExceptionDispatchInfo.Capture(unexpectedFailure).Throw();
        }

        // Every fault was an expected data-loading failure already represented by the primary result.
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
}
