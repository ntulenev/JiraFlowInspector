using System.Globalization;

using Spectre.Console;

namespace JiraMetrics.Presentation;

/// <summary>
/// Provides formatting helpers for Spectre.Console output.
/// </summary>
internal static class SpectrePresentationFormatting
{
    public static string FormatReleaseCell(string value, bool isHotFix)
    {
        var escaped = Markup.Escape(value);
        return isHotFix ? $"[red]{escaped}[/]" : escaped;
    }

    public static string FormatExecutionDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromSeconds(1))
        {
            return $"{duration.TotalMilliseconds:0} ms";
        }

        if (duration < TimeSpan.FromMinutes(1))
        {
            return $"{duration.TotalSeconds:0.##} s";
        }

        return duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    public static string FormatBytes(long bytes)
    {
        const long kilobyte = 1024;
        const long megabyte = 1024 * kilobyte;

        if (bytes >= megabyte)
        {
            return $"{bytes / (double)megabyte:0.##} MB";
        }

        if (bytes >= kilobyte)
        {
            return $"{bytes / (double)kilobyte:0.##} KB";
        }

        return $"{bytes} B";
    }
}
