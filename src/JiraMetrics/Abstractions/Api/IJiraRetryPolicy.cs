using System.Net;

namespace JiraMetrics.Abstractions.Api;

/// <summary>
/// Defines retry behavior for Jira transport requests.
/// </summary>
public interface IJiraRetryPolicy
{
    /// <summary>
    /// Determines whether a retry should occur and returns the delay.
    /// </summary>
    /// <param name="retryAttempt">1-based retry attempt count.</param>
    /// <param name="statusCode">HTTP status code, if available.</param>
    /// <param name="exception">Exception, if available.</param>
    /// <param name="delay">Delay before the retry.</param>
    /// <returns><see langword="true"/> when the request should be retried.</returns>
    bool TryGetDelay(int retryAttempt, HttpStatusCode? statusCode, Exception? exception, out TimeSpan delay);
}

