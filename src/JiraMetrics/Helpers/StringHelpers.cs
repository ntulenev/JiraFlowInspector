namespace JiraMetrics.Helpers;

/// <summary>
/// String helper extensions.
/// </summary>
public static class StringHelpers
{
    /// <summary>
    /// Escapes characters for JQL string literals.
    /// </summary>
    /// <param name="value">Input string.</param>
    /// <returns>Escaped string.</returns>
    public static string EscapeJqlString(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
