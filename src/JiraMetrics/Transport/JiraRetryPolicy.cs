using System.Net;
using System.Security.Cryptography;

using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Transport;

/// <summary>
/// Default retry policy for Jira transport requests.
/// </summary>
public sealed class JiraRetryPolicy : IJiraRetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">Jira configuration options.</param>
    public JiraRetryPolicy(IOptions<JiraOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public bool TryGetDelay(
        int retryAttempt,
        HttpStatusCode? statusCode,
        TimeSpan? serverDelay,
        Exception? exception,
        out TimeSpan delay)
    {
        if (retryAttempt <= 0 || retryAttempt > _options.RetryCount)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        if (exception is not (HttpRequestException or TimeoutException)
            && (statusCode is null || !IsRetryable(statusCode.Value)))
        {
            delay = TimeSpan.Zero;
            return false;
        }

        if (serverDelay.HasValue && serverDelay.Value > TimeSpan.Zero)
        {
            delay = serverDelay.Value > _maxServerDelay
                ? _maxServerDelay
                : serverDelay.Value;
            return true;
        }

        var exponent = Math.Min(retryAttempt - 1, MAX_EXPONENT);
        var exponentialDelayMs = Math.Min(
            MAX_BACKOFF_MS,
            BASE_DELAY_MS * (1 << exponent));
        var jitterMs = RandomNumberGenerator.GetInt32(0, MAX_JITTER_MS + 1);
        delay = TimeSpan.FromMilliseconds(exponentialDelayMs + jitterMs);
        return true;
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || code >= 500;
    }

    private const int BASE_DELAY_MS = 200;
    private const int MAX_BACKOFF_MS = 30_000;
    private const int MAX_JITTER_MS = 100;
    private const int MAX_EXPONENT = 16;
    private static readonly TimeSpan _maxServerDelay = TimeSpan.FromMinutes(2);
    private readonly JiraOptions _options;
}

