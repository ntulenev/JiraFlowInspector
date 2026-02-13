namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a user display name.
/// </summary>
public readonly record struct UserDisplayName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDisplayName"/> struct.
    /// </summary>
    /// <param name="value">Display name value.</param>
    public UserDisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets display name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns display name text.
    /// </summary>
    /// <returns>Display name text.</returns>
    public override string ToString() => Value;
}
