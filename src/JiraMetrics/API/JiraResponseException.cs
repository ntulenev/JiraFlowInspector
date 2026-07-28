namespace JiraMetrics.API;

/// <summary>
/// Represents a missing or invalid Jira API response.
/// </summary>
public sealed class JiraResponseException : JiraDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraResponseException"/> class.
    /// </summary>
    public JiraResponseException()
        : this("The Jira response is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraResponseException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public JiraResponseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraResponseException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Exception that caused this failure.</param>
    public JiraResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
