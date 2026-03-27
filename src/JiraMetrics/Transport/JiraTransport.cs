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
    public JiraTransport(HttpClient http, IJiraRetryPolicy retryPolicy, ISerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(serializer);

        _http = http;
        _retryPolicy = retryPolicy;
        _serializer = serializer;
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
            try
            {
                using var request = requestFactory(url);
                using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return _serializer.Deserialize<TDto>(json);
                }

                if (_retryPolicy.TryGetDelay(attempt + 1, response.StatusCode, null, out var delay))
                {
                    attempt++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Jira API error {(int)response.StatusCode} {response.ReasonPhrase}. Url={url}. Body={body}");
            }
            catch (HttpRequestException ex) when (_retryPolicy.TryGetDelay(attempt + 1, null, ex, out var delay))
            {
                attempt++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private readonly HttpClient _http;
    private readonly IJiraRetryPolicy _retryPolicy;
    private readonly ISerializer _serializer;
}
