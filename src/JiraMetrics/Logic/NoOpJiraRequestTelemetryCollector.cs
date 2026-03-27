using JiraMetrics.Models;

namespace JiraMetrics.Logic;

internal sealed class NoOpJiraRequestTelemetryCollector : IJiraRequestTelemetryCollector
{
    public static NoOpJiraRequestTelemetryCollector Instance { get; } = new();

    public void Reset()
    {
    }

    public void Record(string method, Uri url, TimeSpan duration, int responseBytes, bool isRetry)
    {
    }

    public JiraRequestTelemetrySummary GetSummary() => new(0, 0, 0, TimeSpan.Zero, []);
}

