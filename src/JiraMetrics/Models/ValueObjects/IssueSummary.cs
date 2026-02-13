namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents issue summary text.
/// </summary>
public readonly record struct IssueSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueSummary"/> struct.
    /// </summary>
    /// <param name="value">Summary value.</param>
    public IssueSummary(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets summary text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns summary text.
    /// </summary>
    /// <returns>Summary text.</returns>
    public override string ToString() => Value;
}
