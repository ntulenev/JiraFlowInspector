using System.Diagnostics;

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
    /// <param name="reportingFacade">Application reporting facade.</param>
    /// <param name="requestTelemetryCollector">Jira transport telemetry collector.</param>
    /// <param name="reportLoader">Pre-analysis report data loader.</param>
    /// <param name="analysisRunner">Analysis and rendering workflow runner.</param>
    internal JiraApplication(
        IJiraApplicationReportingFacade reportingFacade,
        IJiraRequestTelemetryCollector requestTelemetryCollector,
        IJiraApplicationReportLoader reportLoader,
        IJiraApplicationAnalysisRunner analysisRunner)
    {
        ArgumentNullException.ThrowIfNull(reportingFacade);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);
        ArgumentNullException.ThrowIfNull(reportLoader);
        ArgumentNullException.ThrowIfNull(analysisRunner);

        _reportingFacade = reportingFacade;
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
            var reportData = await _reportLoader.TryLoadAsync(cancellationToken).ConfigureAwait(false);
            if (reportData is null)
            {
                return;
            }

            await _analysisRunner.RunAsync(reportData, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            totalStopwatch.Stop();
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowExecutionSummary(
                totalStopwatch.Elapsed,
                _requestTelemetryCollector.GetSummary());
        }
    }

    private async Task EnsureReportAccessAsync(CancellationToken cancellationToken)
    {
        _reportingFacade.ShowAuthenticationStarted();

        try
        {
            var user = await _reportLoader.GetReportUserAsync(cancellationToken).ConfigureAwait(false);
            _reportingFacade.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _reportingFacade.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }
    }
    private readonly IJiraApplicationReportingFacade _reportingFacade;
    private readonly IJiraRequestTelemetryCollector _requestTelemetryCollector;
    private readonly IJiraApplicationReportLoader _reportLoader;
    private readonly IJiraApplicationAnalysisRunner _analysisRunner;
}

