namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents Jira base URL.
/// </summary>
public readonly record struct JiraBaseUrl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraBaseUrl"/> struct.
    /// </summary>
    /// <param name="value">Base URL value.</param>
    public JiraBaseUrl(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed))
        {
            throw new ArgumentException("Base URL must be a valid absolute URI.", nameof(value));
        }

        Value = parsed.ToString().TrimEnd('/');
    }

    /// <summary>
    /// Gets base URL value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns base URL value.
    /// </summary>
    /// <returns>Base URL value.</returns>
    public override string ToString() => Value;
}
