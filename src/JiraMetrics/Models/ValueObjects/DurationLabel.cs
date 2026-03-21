using System.Globalization;

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
    /// Creates a formatted duration label.
    /// </summary>
    /// <param name="duration">Duration value.</param>
    /// <param name="showTimeCalculationsInHoursOnly">Whether duration should be formatted strictly in hours.</param>
    /// <returns>Duration label.</returns>
    public static DurationLabel FromDuration(TimeSpan duration, bool showTimeCalculationsInHoursOnly = false)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (showTimeCalculationsInHoursOnly)
        {
            return new DurationLabel($"{duration.TotalHours.ToString("0.##", CultureInfo.InvariantCulture)}h");
        }

        var days = (int)duration.TotalDays;
        var hours = duration.Hours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;

        var parts = new List<string>();
        if (days > 0)
        {
            parts.Add($"{days}d");
        }

        if (hours > 0)
        {
            parts.Add($"{hours}h");
        }

        if (minutes > 0)
        {
            parts.Add($"{minutes}m");
        }

        if (parts.Count == 0)
        {
            parts.Add($"{seconds}s");
        }

        return new DurationLabel(string.Join(" ", parts));
    }

    /// <summary>
    /// Returns duration label text.
    /// </summary>
    /// <returns>Duration label text.</returns>
    public override string ToString() => Value;
}
