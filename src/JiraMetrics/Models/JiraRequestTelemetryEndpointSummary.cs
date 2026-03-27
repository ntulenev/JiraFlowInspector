namespace JiraMetrics.Models;

/// <summary>
/// Aggregated Jira transport metrics per endpoint.
/// </summary>
public sealed record JiraRequestTelemetryEndpointSummary(
    string Method,
    string Endpoint,
    int RequestCount,
    int RetryCount,
    long ResponseBytes,
    TimeSpan TotalDuration,
    TimeSpan MaxDuration);
