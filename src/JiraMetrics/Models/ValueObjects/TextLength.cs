using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a positive maximum text length.
/// </summary>
public readonly record struct TextLength
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextLength"/> struct.
    /// </summary>
    /// <param name="value">Length value.</param>
    public TextLength(int value)
    {
        if (value < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Text length must be at least 4.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets text length value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Returns text length as string.
    /// </summary>
    /// <returns>Text length value.</returns>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
