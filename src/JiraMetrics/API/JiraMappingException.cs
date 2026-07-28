namespace JiraMetrics.API;

/// <summary>
/// Represents a Jira payload that cannot be mapped to an application model.
/// </summary>
public sealed class JiraMappingException : JiraDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraMappingException"/> class.
    /// </summary>
    public JiraMappingException()
        : this("The Jira payload cannot be mapped.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraMappingException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public JiraMappingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraMappingException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Exception that caused this failure.</param>
    public JiraMappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
