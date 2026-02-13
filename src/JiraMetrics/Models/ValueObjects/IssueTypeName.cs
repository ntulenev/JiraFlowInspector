namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira issue type name.
/// </summary>
public readonly record struct IssueTypeName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTypeName"/> struct.
    /// </summary>
    /// <param name="value">Issue type name text.</param>
    public IssueTypeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets unknown issue type value.
    /// </summary>
    public static IssueTypeName Unknown => new("Unknown");

    /// <summary>
    /// Creates issue type from nullable source value.
    /// </summary>
    /// <param name="value">Source value that may be null or whitespace.</param>
    /// <returns>Parsed issue type or <see cref="Unknown"/>.</returns>
    public static IssueTypeName FromNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Unknown
            : new IssueTypeName(value.Trim());
    }

    /// <summary>
    /// Gets issue type name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns issue type name text.
    /// </summary>
    /// <returns>Issue type name text.</returns>
    public override string ToString() => Value;
}
