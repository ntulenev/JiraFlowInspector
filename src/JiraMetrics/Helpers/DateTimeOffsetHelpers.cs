using System.Globalization;

namespace JiraMetrics.Helpers;

/// <summary>
/// DateTimeOffset helper extensions.
/// </summary>
public static class DateTimeOffsetHelpers
{
    /// <summary>
    /// Parses a nullable string into a nullable <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">Raw string value.</param>
    /// <returns>Parsed date-time value or <c>null</c>.</returns>
    public static DateTimeOffset? ParseNullableDateTimeOffset(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }
}
