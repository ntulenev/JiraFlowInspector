using JiraMetrics.Models;

namespace JiraMetrics.Transport;

/// <summary>
/// In-memory Jira transport telemetry collector.
/// </summary>
public sealed class JiraRequestTelemetryCollector : IJiraRequestTelemetryCollector
{
    /// <inheritdoc />
    public void Reset()
    {
        lock (_sync)
        {
            _requestCount = 0;
            _retryCount = 0;
            _responseBytes = 0;
            _totalDuration = TimeSpan.Zero;
            _endpointMetrics.Clear();
        }
    }

    /// <inheritdoc />
    public void Record(string method, Uri url, TimeSpan duration, int responseBytes, bool isRetry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(url);

        var endpoint = NormalizeEndpoint(url);
        var key = $"{method.ToUpperInvariant()} {endpoint}";

        lock (_sync)
        {
            _requestCount++;
            if (isRetry)
            {
                _retryCount++;
            }

            _responseBytes += responseBytes;
            _totalDuration += duration;

            if (!_endpointMetrics.TryGetValue(key, out var metric))
            {
                metric = new EndpointMetrics(method.ToUpperInvariant(), endpoint);
                _endpointMetrics[key] = metric;
            }

            metric.RequestCount++;
            if (isRetry)
            {
                metric.RetryCount++;
            }

            metric.ResponseBytes += responseBytes;
            metric.TotalDuration += duration;
            if (duration > metric.MaxDuration)
            {
                metric.MaxDuration = duration;
            }
        }
    }

    /// <inheritdoc />
    public JiraRequestTelemetrySummary GetSummary()
    {
        lock (_sync)
        {
            return new JiraRequestTelemetrySummary(
                _requestCount,
                _retryCount,
                _responseBytes,
                _totalDuration,
                [.. _endpointMetrics.Values
                    .Select(static metric => new JiraRequestTelemetryEndpointSummary(
                        metric.Method,
                        metric.Endpoint,
                        metric.RequestCount,
                        metric.RetryCount,
                        metric.ResponseBytes,
                        metric.TotalDuration,
                        metric.MaxDuration))
                    .OrderByDescending(static metric => metric.TotalDuration)
                    .ThenByDescending(static metric => metric.RequestCount)
                    .ThenBy(static metric => metric.Endpoint, StringComparer.OrdinalIgnoreCase)]);
        }
    }

    private static string NormalizeEndpoint(Uri url)
    {
        var path = url.IsAbsoluteUri
            ? url.AbsolutePath
            : url.OriginalString.Split('?', 2)[0];
        return string.IsNullOrWhiteSpace(path) ? "/" : path;
    }

    private sealed class EndpointMetrics
    {
        public EndpointMetrics(string method, string endpoint)
        {
            Method = method;
            Endpoint = endpoint;
        }

        public string Method { get; }

        public string Endpoint { get; }

        public int RequestCount { get; set; }

        public int RetryCount { get; set; }

        public long ResponseBytes { get; set; }

        public TimeSpan TotalDuration { get; set; }

        public TimeSpan MaxDuration { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<string, EndpointMetrics> _endpointMetrics =
        new(StringComparer.OrdinalIgnoreCase);
    private int _requestCount;
    private int _retryCount;
    private long _responseBytes;
    private TimeSpan _totalDuration;
}

