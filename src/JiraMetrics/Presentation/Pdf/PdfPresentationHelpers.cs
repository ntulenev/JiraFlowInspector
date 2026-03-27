using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Helper styles and formatting for PDF rendering.
/// </summary>
internal static class PdfPresentationHelpers
{

    public static IContainer StyleHeaderCell(IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return container
            .Border(1)
            .Background(HEADER_BACKGROUND_COLOR_HEX)
            .PaddingHorizontal(6)
            .PaddingVertical(4)
            .DefaultTextStyle(static style => style.FontSize(9).FontColor(HEADER_TEXT_COLOR_HEX).SemiBold());
    }

    public static IContainer StyleBodyCell(IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return container
            .Border(1)
            .PaddingHorizontal(6)
            .PaddingVertical(4)
            .DefaultTextStyle(static style => style.FontSize(8));
    }

    public static string BuildIssueBrowseUrl(JiraBaseUrl baseUrl, IssueKey issueKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueKey.Value);

        return $"{baseUrl.Value}/browse/{issueKey.Value}";
    }

    public static string ToDurationLabel(TimeSpan duration, bool showTimeCalculationsInHoursOnly = false) =>
        DurationLabel.FromDuration(duration, showTimeCalculationsInHoursOnly).Value;
    private const string HEADER_BACKGROUND_COLOR_HEX = "#1f2937";
    private const string HEADER_TEXT_COLOR_HEX = "#f9fafb";
}
