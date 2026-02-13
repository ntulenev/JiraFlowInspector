namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents formatted duration text.
/// </summary>
public readonly record struct DurationLabel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurationLabel"/> struct.
    /// </summary>
    /// <param name="value">Duration label value.</param>
    public DurationLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets duration label text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns duration label text.
    /// </summary>
    /// <returns>Duration label text.</returns>
    public override string ToString() => Value;
}
