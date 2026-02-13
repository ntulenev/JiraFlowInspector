namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a machine-readable transition path key.
/// </summary>
public readonly record struct PathKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathKey"/> struct.
    /// </summary>
    /// <param name="value">Path key text.</param>
    public PathKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets path key text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns path key text.
    /// </summary>
    /// <returns>Path key text.</returns>
    public override string ToString() => Value;
}
