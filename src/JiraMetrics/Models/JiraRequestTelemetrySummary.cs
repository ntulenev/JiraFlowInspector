namespace JiraMetrics.Models;

/// <summary>
/// Aggregated Jira transport metrics for the current run.
/// </summary>
public sealed record JiraRequestTelemetrySummary(
    int RequestCount,
    int RetryCount,
    long ResponseBytes,
    TimeSpan TotalDuration,
    IReadOnlyList<JiraRequestTelemetryEndpointSummary> Endpoints);
