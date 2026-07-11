namespace JiraMetrics.Models;

/// <summary>
/// Aggregated Jira transport metrics for the current run.
/// </summary>
/// <param name="RequestCount">The <paramref name="RequestCount"/> value.</param>
/// <param name="RetryCount">The <paramref name="RetryCount"/> value.</param>
/// <param name="ResponseBytes">The <paramref name="ResponseBytes"/> value.</param>
/// <param name="TotalDuration">The <paramref name="TotalDuration"/> value.</param>
/// <param name="Endpoints">The <paramref name="Endpoints"/> value.</param>
public sealed record JiraRequestTelemetrySummary(
    int RequestCount,
    int RetryCount,
    long ResponseBytes,
    TimeSpan TotalDuration,
    IReadOnlyList<JiraRequestTelemetryEndpointSummary> Endpoints);
