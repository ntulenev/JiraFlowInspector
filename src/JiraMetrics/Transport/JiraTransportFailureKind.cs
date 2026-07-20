namespace JiraMetrics.Transport;

/// <summary>
/// Classifies a failed Jira transport operation.
/// </summary>
public enum JiraTransportFailureKind
{
    /// <summary>
    /// The request failed before an HTTP response was received.
    /// </summary>
    Network,

    /// <summary>
    /// The request exceeded the configured HTTP timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Jira rejected the request because the caller exceeded a rate limit.
    /// </summary>
    RateLimited,

    /// <summary>
    /// Jira returned a transient or permanent server-side error.
    /// </summary>
    ServerError,

    /// <summary>
    /// Jira rejected the request as invalid or unauthorized.
    /// </summary>
    ClientError
}
