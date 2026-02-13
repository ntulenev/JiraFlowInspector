namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira project key.
/// </summary>
public readonly record struct ProjectKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectKey"/> struct.
    /// </summary>
    /// <param name="value">Project key text.</param>
    public ProjectKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets project key text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns project key text.
    /// </summary>
    /// <returns>Project key text.</returns>
    public override string ToString() => Value;
}
