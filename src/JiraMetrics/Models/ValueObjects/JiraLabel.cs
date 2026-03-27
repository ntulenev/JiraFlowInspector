namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a Jira label.
/// </summary>
public readonly record struct JiraLabel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraLabel"/> struct.
    /// </summary>
    /// <param name="value">Label text.</param>
    public JiraLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets label text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns label text.
    /// </summary>
    /// <returns>Label text.</returns>
    public override string ToString() => Value;
}
