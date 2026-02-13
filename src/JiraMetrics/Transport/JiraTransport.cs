using System.Text.Json;

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
    public JiraTransport(HttpClient http, IJiraRetryPolicy retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        _http = http;
        _retryPolicy = retryPolicy;
    }

    /// <inheritdoc />
    public async Task<TDto?> GetAsync<TDto>(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        var attempt = 0;

        while (true)
        {
            try
            {
                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    return JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
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

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IJiraRetryPolicy _retryPolicy;
}
