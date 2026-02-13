using System.Net;

using JiraMetrics.Abstractions;
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
    public bool TryGetDelay(int retryAttempt, HttpStatusCode? statusCode, Exception? exception, out TimeSpan delay)
    {
        if (retryAttempt <= 0 || retryAttempt > _options.RetryCount)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        if (exception is HttpRequestException)
        {
            delay = TimeSpan.FromMilliseconds(BASE_DELAY_MS * retryAttempt);
            return true;
        }

        if (statusCode is not null && IsRetryable(statusCode.Value))
        {
            delay = TimeSpan.FromMilliseconds(BASE_DELAY_MS * retryAttempt);
            return true;
        }

        delay = TimeSpan.Zero;
        return false;
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode == HttpStatusCode.TooManyRequests || code >= 500;
    }

    private const int BASE_DELAY_MS = 200;
    private readonly JiraOptions _options;
}
