namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a workflow status name.
/// </summary>
public readonly record struct StatusName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusName"/> struct.
    /// </summary>
    /// <param name="value">Status name text.</param>
    public StatusName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets the unknown status value.
    /// </summary>
    public static StatusName Unknown => new("Unknown");

    /// <summary>
    /// Creates a status from nullable source value.
    /// </summary>
    /// <param name="value">Source value that may be null or whitespace.</param>
    /// <returns>Parsed status or <see cref="Unknown"/>.</returns>
    public static StatusName FromNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Unknown
            : new StatusName(value.Trim());
    }

    /// <summary>
    /// Gets status name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns status name text.
    /// </summary>
    /// <returns>Status name text.</returns>
    public override string ToString() => Value;
}
