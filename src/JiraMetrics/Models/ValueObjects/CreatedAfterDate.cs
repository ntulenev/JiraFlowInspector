using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents optional lower bound date for issue creation filtering.
/// </summary>
public readonly record struct CreatedAfterDate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAfterDate"/> struct.
    /// </summary>
    /// <param name="value">Date value in yyyy-MM-dd format.</param>
    public CreatedAfterDate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedValue))
        {
            throw new ArgumentException("Created after date must match yyyy-MM-dd format.", nameof(value));
        }

        Value = parsedValue;
    }

    /// <summary>
    /// Gets date value.
    /// </summary>
    public DateOnly Value { get; }

    /// <summary>
    /// Returns date value in yyyy-MM-dd format.
    /// </summary>
    /// <returns>Date value string.</returns>
    public override string ToString() => Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
