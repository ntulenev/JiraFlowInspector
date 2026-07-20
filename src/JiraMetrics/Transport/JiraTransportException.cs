using System.Net;

namespace JiraMetrics.Transport;

/// <summary>
/// Represents a classified Jira HTTP or network failure.
/// </summary>
public sealed class JiraTransportException : HttpRequestException
{
    /// <summary>
    /// Initializes a transport exception with a default message.
    /// </summary>
    public JiraTransportException()
        : this("A Jira transport operation failed.")
    {
    }

    /// <summary>
    /// Initializes a transport exception with the provided message.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public JiraTransportException(string message)
        : base(message)
    {
        FailureKind = JiraTransportFailureKind.Network;
        Endpoint = "/";
    }

    /// <summary>
    /// Initializes a transport exception with the provided message and cause.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Underlying exception.</param>
    public JiraTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailureKind = JiraTransportFailureKind.Network;
        Endpoint = "/";
    }

    internal JiraTransportException(
        JiraTransportFailureKind failureKind,
        string endpoint,
        HttpStatusCode? statusCode,
        TimeSpan? retryAfter,
        Exception? innerException = null)
        : base(BuildMessage(failureKind, endpoint, statusCode), innerException, statusCode)
    {
        FailureKind = failureKind;
        Endpoint = endpoint;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the classified failure kind.
    /// </summary>
    public JiraTransportFailureKind FailureKind { get; }

    /// <summary>
    /// Gets the request endpoint without its query string.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the delay requested by Jira, when supplied through <c>Retry-After</c>.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    private static string BuildMessage(
        JiraTransportFailureKind failureKind,
        string endpoint,
        HttpStatusCode? statusCode)
    {
        var statusText = statusCode is null
            ? "without an HTTP response"
            : $"with status {(int)statusCode.Value} ({statusCode.Value})";
        return $"Jira request to '{endpoint}' failed {statusText}. Failure kind: {failureKind}.";
    }
}
