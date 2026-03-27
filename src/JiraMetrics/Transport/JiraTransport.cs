using System.Diagnostics;
using System.Text;

using JiraMetrics.Abstractions;

namespace JiraMetrics.Transport;

/// <summary>
/// HTTP transport implementation for Jira API.
/// </summary>
public sealed class JiraTransport : IJiraTransport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraTransport"/> class.
    /// </summary>
    /// <param name="http">HTTP client instance.</param>
    /// <param name="retryPolicy">Retry policy instance.</param>
    /// <param name="serializer">Serializer instance.</param>
    /// <param name="telemetryCollector">Transport telemetry collector.</param>
    public JiraTransport(
        HttpClient http,
        IJiraRetryPolicy retryPolicy,
        ISerializer serializer,
        IJiraRequestTelemetryCollector telemetryCollector)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(telemetryCollector);

        _http = http;
        _retryPolicy = retryPolicy;
        _serializer = serializer;
        _telemetryCollector = telemetryCollector;
    }

    /// <inheritdoc />
    public async Task<TDto?> GetAsync<TDto>(Uri url, CancellationToken cancellationToken)
        => await SendAsync<TDto>(
            url,
            static requestUrl => new HttpRequestMessage(HttpMethod.Get, requestUrl),
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TDto?> PostAsync<TRequest, TDto>(
        Uri url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(request);

        var json = _serializer.Serialize(request);
        return await SendAsync<TDto>(
            url,
            requestUrl =>
            {
                var message = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return message;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TDto?> SendAsync<TDto>(
        Uri url,
        Func<Uri, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(requestFactory);

        var attempt = 0;

        while (true)
        {
            using var request = requestFactory(url);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _telemetryCollector.Record(
                    request.Method.Method,
                    url,
                    stopwatch.Elapsed,
                    Encoding.UTF8.GetByteCount(body),
                    isRetry: attempt > 0);

                if (response.IsSuccessStatusCode)
                {
                    return _serializer.Deserialize<TDto>(body);
                }

                if (_retryPolicy.TryGetDelay(attempt + 1, response.StatusCode, null, out var delay))
                {
                    attempt++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new HttpRequestException(
                    $"Jira API error {(int)response.StatusCode} {response.ReasonPhrase}. Url={url}. Body={body}");
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _telemetryCollector.Record(
                    request.Method.Method,
                    url,
                    stopwatch.Elapsed,
                    responseBytes: 0,
                    isRetry: attempt > 0);

                if (_retryPolicy.TryGetDelay(attempt + 1, null, ex, out var delay))
                {
                    attempt++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw;
            }
        }
    }

    private readonly HttpClient _http;
    private readonly IJiraRetryPolicy _retryPolicy;
    private readonly ISerializer _serializer;
    private readonly IJiraRequestTelemetryCollector _telemetryCollector;
}
