namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents an error message.
/// </summary>
public readonly record struct ErrorMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorMessage"/> struct.
    /// </summary>
    /// <param name="value">Error message text.</param>
    public ErrorMessage(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Creates an error message from an exception.
    /// </summary>
    /// <param name="exception">Exception instance.</param>
    /// <returns>Error message.</returns>
    public static ErrorMessage FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ErrorMessage(string.IsNullOrWhiteSpace(exception.Message)
            ? "Unknown error."
            : exception.Message);
    }

    /// <summary>
    /// Gets error message text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns error message text.
    /// </summary>
    /// <returns>Error message text.</returns>
    public override string ToString() => Value;
}
