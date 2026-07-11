namespace JiraMetrics.Models;

/// <summary>
/// Aggregated Jira transport metrics per endpoint.
/// </summary>
/// <param name="Method">The <paramref name="Method"/> value.</param>
/// <param name="Endpoint">The <paramref name="Endpoint"/> value.</param>
/// <param name="RequestCount">The <paramref name="RequestCount"/> value.</param>
/// <param name="RetryCount">The <paramref name="RetryCount"/> value.</param>
/// <param name="ResponseBytes">The <paramref name="ResponseBytes"/> value.</param>
/// <param name="TotalDuration">The <paramref name="TotalDuration"/> value.</param>
/// <param name="MaxDuration">The <paramref name="MaxDuration"/> value.</param>
public sealed record JiraRequestTelemetryEndpointSummary(
    string Method,
    string Endpoint,
    int RequestCount,
    int RetryCount,
    long ResponseBytes,
    TimeSpan TotalDuration,
    TimeSpan MaxDuration);
