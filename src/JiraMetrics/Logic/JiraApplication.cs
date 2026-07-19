using System.Diagnostics;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Default application workflow implementation.
/// </summary>
public sealed class JiraApplication : IJiraApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="statusPresenter">Application status presenter.</param>
    /// <param name="requestTelemetryCollector">Jira transport telemetry collector.</param>
    /// <param name="reportLoader">Pre-analysis report data loader.</param>
    /// <param name="analysisRunner">Analysis and rendering workflow runner.</param>
    internal JiraApplication(
        IJiraStatusPresenter statusPresenter,
        IJiraRequestTelemetryCollector requestTelemetryCollector,
        IJiraApplicationReportLoader reportLoader,
        IJiraApplicationAnalysisRunner analysisRunner)
    {
        ArgumentNullException.ThrowIfNull(statusPresenter);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);
        ArgumentNullException.ThrowIfNull(reportLoader);
        ArgumentNullException.ThrowIfNull(analysisRunner);

        _statusPresenter = statusPresenter;
        _requestTelemetryCollector = requestTelemetryCollector;
        _reportLoader = reportLoader;
        _analysisRunner = analysisRunner;
    }

    /// <summary>
    /// Executes the application flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _requestTelemetryCollector.Reset();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            await EnsureReportAccessAsync(cancellationToken).ConfigureAwait(false);
            var loadResult = await _reportLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loadResult is not ReportLoadResult.Success success)
            {
                return;
            }

            await _analysisRunner.RunAsync(success.ReportData, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            totalStopwatch.Stop();
            _statusPresenter.ShowSpacer();
            _statusPresenter.ShowExecutionSummary(
                totalStopwatch.Elapsed,
                _requestTelemetryCollector.GetSummary());
        }
    }

    private async Task EnsureReportAccessAsync(CancellationToken cancellationToken)
    {
        _statusPresenter.ShowAuthenticationStarted();

        try
        {
            var user = await _reportLoader.GetReportUserAsync(cancellationToken).ConfigureAwait(false);
            _statusPresenter.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _statusPresenter.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }
    }
    private readonly IJiraStatusPresenter _statusPresenter;
    private readonly IJiraRequestTelemetryCollector _requestTelemetryCollector;
    private readonly IJiraApplicationReportLoader _reportLoader;
    private readonly IJiraApplicationAnalysisRunner _analysisRunner;
}

