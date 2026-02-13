namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents Jira API token.
/// </summary>
public readonly record struct JiraApiToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiToken"/> struct.
    /// </summary>
    /// <param name="value">Token value.</param>
    public JiraApiToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets token value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns token value.
    /// </summary>
    /// <returns>Token value.</returns>
    public override string ToString() => Value;
}
