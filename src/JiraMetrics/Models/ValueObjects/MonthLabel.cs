using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a month label in yyyy-MM format.
/// </summary>
public readonly partial record struct MonthLabel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthLabel"/> struct.
    /// </summary>
    /// <param name="value">Month label value.</param>
    public MonthLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!MyRegex().IsMatch(value.Trim()))
        {
            throw new ArgumentException("Month label must match yyyy-MM format.", nameof(value));
        }

        Value = value.Trim();
    }

    /// <summary>
    /// Creates current UTC month label.
    /// </summary>
    /// <returns>Current UTC month label.</returns>
    public static MonthLabel CurrentUtc() => new(DateTimeOffset.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets inclusive month start and exclusive month end range.
    /// </summary>
    /// <returns>Month range.</returns>
    public (DateOnly Start, DateOnly EndExclusive) GetMonthRange()
    {
        if (!DateOnly.TryParseExact(
                $"{Value}-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var start))
        {
            throw new ArgumentException($"Invalid MonthLabel '{Value}'. Expected yyyy-MM.");
        }

        return (start, start.AddMonths(1));
    }

    /// <summary>
    /// Gets month label value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns month label value.
    /// </summary>
    /// <returns>Month label value.</returns>
    public override string ToString() => Value;
    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{4}-\d{2}$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
