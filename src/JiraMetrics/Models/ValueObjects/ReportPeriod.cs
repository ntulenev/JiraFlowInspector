using System.Globalization;

namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a report period defined either by month label or explicit date range.
/// </summary>
public readonly record struct ReportPeriod
{
    private static readonly string[] _supportedDateFormats =
    [
        "dd.MM.yyyy",
        "yyyy-MM-dd"
    ];

    private ReportPeriod(
        DateOnly start,
        DateOnly endInclusive,
        string label,
        MonthLabel? monthLabel)
    {
        if (endInclusive < start)
        {
            throw new ArgumentException("Report period end date must be greater than or equal to start date.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        Start = start;
        EndInclusive = endInclusive;
        Label = label.Trim();
        MonthLabel = monthLabel;
    }

    /// <summary>
    /// Gets report period start date (inclusive).
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets report period end date (inclusive).
    /// </summary>
    public DateOnly EndInclusive { get; }

    /// <summary>
    /// Gets report period end date (exclusive).
    /// </summary>
    public DateOnly EndExclusive => EndInclusive.AddDays(1);

    /// <summary>
    /// Gets period label for UI output.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets month label when period is month-based.
    /// </summary>
    public MonthLabel? MonthLabel { get; }

    /// <summary>
    /// Gets whether the period comes from month label configuration.
    /// </summary>
    public bool IsMonthBased => MonthLabel is not null;

    /// <summary>
    /// Creates a report period for the current UTC month.
    /// </summary>
    /// <returns>Current UTC month report period.</returns>
    public static ReportPeriod CurrentUtcMonth() => FromMonthLabel(JiraMetrics.Models.ValueObjects.MonthLabel.CurrentUtc());

    /// <summary>
    /// Creates a report period from month label.
    /// </summary>
    /// <param name="monthLabel">Month label.</param>
    /// <returns>Month-based report period.</returns>
    public static ReportPeriod FromMonthLabel(MonthLabel monthLabel)
    {
        var (start, endExclusive) = monthLabel.GetMonthRange();
        return new ReportPeriod(start, endExclusive.AddDays(-1), monthLabel.Value, monthLabel);
    }

    /// <summary>
    /// Creates a report period from explicit start and end dates.
    /// </summary>
    /// <param name="start">Start date (inclusive).</param>
    /// <param name="endInclusive">End date (inclusive).</param>
    /// <returns>Date-range report period.</returns>
    public static ReportPeriod FromDateRange(DateOnly start, DateOnly endInclusive) =>
        new(
            start,
            endInclusive,
            $"{start:dd.MM.yyyy} - {endInclusive:dd.MM.yyyy}",
            monthLabel: null);

    /// <summary>
    /// Tries to parse configured date in supported input formats.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="date">Parsed date.</param>
    /// <returns><see langword="true"/> when parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParseConfiguredDate(string? value, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = default;
            return false;
        }

        return DateOnly.TryParseExact(
            value.Trim(),
            _supportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// Parses configured date in supported input formats.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <param name="parameterName">Parameter name for exception.</param>
    /// <returns>Parsed date.</returns>
    public static DateOnly ParseConfiguredDate(string value, string parameterName)
    {
        if (!TryParseConfiguredDate(value, out var date))
        {
            throw new ArgumentException(
                "Date must match dd.MM.yyyy or yyyy-MM-dd format.",
                parameterName);
        }

        return date;
    }

    /// <summary>
    /// Returns period label.
    /// </summary>
    /// <returns>Period label.</returns>
    public override string ToString() => Label;
}
