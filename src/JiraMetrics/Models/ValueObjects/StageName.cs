namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a required path stage name used for filtering.
/// </summary>
public readonly record struct StageName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageName"/> struct.
    /// </summary>
    /// <param name="value">Stage name text.</param>
    public StageName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets stage name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns stage name text.
    /// </summary>
    /// <returns>Stage name text.</returns>
    public override string ToString() => Value;
}
