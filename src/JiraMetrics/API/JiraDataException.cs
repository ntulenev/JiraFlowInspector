namespace JiraMetrics.API;

/// <summary>
/// Represents invalid or incomplete data received from Jira.
/// </summary>
public abstract class JiraDataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraDataException"/> class.
    /// </summary>
    protected JiraDataException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraDataException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    protected JiraDataException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraDataException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Exception that caused this failure.</param>
    protected JiraDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
