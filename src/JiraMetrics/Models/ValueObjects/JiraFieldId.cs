namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira field id.
/// </summary>
public readonly record struct JiraFieldId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraFieldId"/> struct.
    /// </summary>
    /// <param name="value">Field id text.</param>
    public JiraFieldId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets field id text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a field id from nullable source value.
    /// </summary>
    /// <param name="value">Source value that may be null or whitespace.</param>
    /// <returns>Parsed field id or null.</returns>
    public static JiraFieldId? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new JiraFieldId(value.Trim());

    /// <summary>
    /// Returns field id text.
    /// </summary>
    /// <returns>Field id text.</returns>
    public override string ToString() => Value;
}
