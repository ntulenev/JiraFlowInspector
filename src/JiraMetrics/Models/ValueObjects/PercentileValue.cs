using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents percentile value in a 0..1 range.
/// </summary>
public readonly record struct PercentileValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PercentileValue"/> struct.
    /// </summary>
    /// <param name="value">Percentile value in range 0..1.</param>
    public PercentileValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Percentile cannot be NaN or Infinity.");
        }

        if (value is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Percentile must be in range 0..1.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets percentile value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Returns percentile text.
    /// </summary>
    /// <returns>Percentile value text.</returns>
    public override string ToString() => Value.ToString("0.###", CultureInfo.InvariantCulture);
}
