namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a human-readable transition path label.
/// </summary>
public readonly record struct PathLabel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathLabel"/> struct.
    /// </summary>
    /// <param name="value">Path label text.</param>
    public PathLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets path label text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns path label text.
    /// </summary>
    /// <returns>Path label text.</returns>
    public override string ToString() => Value;
}
