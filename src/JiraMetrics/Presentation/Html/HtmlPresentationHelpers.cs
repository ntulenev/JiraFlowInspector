using System.Globalization;
using System.Net;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Helper methods for HTML report rendering.
/// </summary>
internal static class HtmlPresentationHelpers
{
    /// <summary>
    /// Encodes text for HTML content.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <returns>HTML-encoded value.</returns>
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>
    /// Encodes text for HTML attributes.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <returns>HTML attribute-encoded value.</returns>
    public static string EncodeAttribute(string? value) => Encode(value).Replace("'", "&#39;", StringComparison.Ordinal);

    /// <summary>
    /// Builds Jira issue browse URL.
    /// </summary>
    /// <param name="baseUrl">Jira base URL.</param>
    /// <param name="issueKey">Issue key.</param>
    /// <returns>Issue browse URL.</returns>
    public static string BuildIssueBrowseUrl(JiraBaseUrl baseUrl, IssueKey issueKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueKey.Value);

        return $"{baseUrl.Value}/browse/{issueKey.Value}";
    }

    /// <summary>
    /// Formats local date and time.
    /// </summary>
    /// <param name="value">Date and time value.</param>
    /// <returns>Formatted date and time.</returns>
    public static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats optional local date and time.
    /// </summary>
    /// <param name="value">Optional date and time value.</param>
    /// <returns>Formatted date and time or dash.</returns>
    public static string FormatDateTime(DateTimeOffset? value) => value.HasValue ? FormatDateTime(value.Value) : "-";

    /// <summary>
    /// Formats date.
    /// </summary>
    /// <param name="value">Date value.</param>
    /// <returns>Formatted date.</returns>
    public static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats integer count.
    /// </summary>
    /// <param name="value">Count value.</param>
    /// <returns>Invariant count text.</returns>
    public static string FormatCount(int value) => value.ToString(CultureInfo.InvariantCulture);
}
