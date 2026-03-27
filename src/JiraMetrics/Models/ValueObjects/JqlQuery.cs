namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira JQL query.
/// </summary>
public readonly record struct JqlQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JqlQuery"/> struct.
    /// </summary>
    /// <param name="value">JQL query text.</param>
    public JqlQuery(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets JQL query text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns JQL query text.
    /// </summary>
    /// <returns>JQL query text.</returns>
    public override string ToString() => Value;
}
