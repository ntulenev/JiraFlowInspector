namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents Jira account email.
/// </summary>
public readonly record struct JiraEmail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraEmail"/> struct.
    /// </summary>
    /// <param name="value">Email value.</param>
    public JiraEmail(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets email value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns email value.
    /// </summary>
    /// <returns>Email value.</returns>
    public override string ToString() => Value;
}
