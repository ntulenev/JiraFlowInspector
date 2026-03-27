using JiraMetrics.Models;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Collects Jira HTTP transport telemetry for the current application run.
/// </summary>
public interface IJiraRequestTelemetryCollector
{
    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    void Reset();

    /// <summary>
    /// Records a completed HTTP attempt.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="url">Request URL.</param>
    /// <param name="duration">Attempt duration.</param>
    /// <param name="responseBytes">Response body size in bytes.</param>
    /// <param name="isRetry">Whether this attempt is a retry.</param>
    void Record(string method, Uri url, TimeSpan duration, int responseBytes, bool isRetry);

    /// <summary>
    /// Builds a snapshot of the aggregated metrics.
    /// </summary>
    JiraRequestTelemetrySummary GetSummary();
}
