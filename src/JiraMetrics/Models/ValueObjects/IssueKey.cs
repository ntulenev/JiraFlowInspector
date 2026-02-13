namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira issue key.
/// </summary>
public readonly record struct IssueKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueKey"/> struct.
    /// </summary>
    /// <param name="value">Issue key text.</param>
    public IssueKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets issue key text value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns issue key text.
    /// </summary>
    /// <returns>Issue key text.</returns>
    public override string ToString() => Value;
}
