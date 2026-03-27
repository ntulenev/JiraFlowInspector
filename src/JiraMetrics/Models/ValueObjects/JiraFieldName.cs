namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira field name.
/// </summary>
public readonly record struct JiraFieldName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraFieldName"/> struct.
    /// </summary>
    /// <param name="value">Field name text.</param>
    public JiraFieldName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets field name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a field name from nullable source value.
    /// </summary>
    /// <param name="value">Source value that may be null or whitespace.</param>
    /// <returns>Parsed field name or null.</returns>
    public static JiraFieldName? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new JiraFieldName(value.Trim());

    /// <summary>
    /// Returns field name text.
    /// </summary>
    /// <returns>Field name text.</returns>
    public override string ToString() => Value;
}
