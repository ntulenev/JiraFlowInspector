namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a normalized Jira field value.
/// </summary>
public readonly record struct JiraFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraFieldValue"/> struct.
    /// </summary>
    /// <param name="value">Field value text.</param>
    public JiraFieldValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets field value text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a field value from nullable source value.
    /// </summary>
    /// <param name="value">Source value that may be null or whitespace.</param>
    /// <returns>Parsed field value or null.</returns>
    public static JiraFieldValue? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new JiraFieldValue(value.Trim());

    /// <summary>
    /// Returns field value text.
    /// </summary>
    /// <returns>Field value text.</returns>
    public override string ToString() => Value;
}
