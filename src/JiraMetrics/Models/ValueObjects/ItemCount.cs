using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a non-negative count value.
/// </summary>
public readonly record struct ItemCount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCount"/> struct.
    /// </summary>
    /// <param name="value">Count value.</param>
    public ItemCount(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Count cannot be negative.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets count value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Returns count text.
    /// </summary>
    /// <returns>Count text.</returns>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
