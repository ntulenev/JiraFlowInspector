using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders optional issue analysis for a configured status transition.
/// </summary>
internal sealed class PdfCustomTransitionAnalysisSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        var settings = reportData.Settings.CustomTransitionAnalysis;
        if (settings is null)
        {
            return;
        }

        var issues = reportData.CustomTransitionIssues;

        _ = column.Item().Text("Custom transition analysis").Bold().FontSize(12);
        _ = column.Item().Text($"Issues with {settings.Label} transition").Bold();

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            return;
        }

        ComposeIssueTable(column, reportData, settings, issues);
        ComposeTransitionP75PerTypeSection(
            column,
            settings,
            reportData.CustomTransitionDuration75PerType,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);
    }

    private static void ComposeIssueTable(
        ColumnDescriptor column,
        JiraReportData reportData,
        CustomTransitionAnalysisSettings settings,
        IReadOnlyList<CustomTransitionIssue> issues)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(74);
                columns.ConstantColumn(74);
                columns.ConstantColumn(64);
                columns.ConstantColumn(44);
                columns.RelativeColumn(4);
                columns.ConstantColumn(82);
                columns.ConstantColumn(90);
                columns.ConstantColumn(90);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Sub-items");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Code");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Summary");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Transition At");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(BuildDurationColumnLabel(settings, reportData.Settings.ShowTimeCalculationsInHoursOnly));
            });

            for (var i = 0; i < issues.Count; i++)
            {
                var item = issues[i];
                var issue = item.Issue;
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(issue.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.SubItemsCount.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.HasPullRequest ? "+" : string.Empty);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.Summary.Truncate(new TextLength(140)).Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(item.TransitionAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(PdfPresentationFormatting.FormatWorkDurationValue(
                        item.Duration,
                        reportData.Settings.ShowTimeCalculationsInHoursOnly));
            }
        });
    }

    private static void ComposeTransitionP75PerTypeSection(
        ColumnDescriptor column,
        CustomTransitionAnalysisSettings settings,
        IReadOnlyList<IssueTypeDuration75Summary> summaries,
        bool showTimeCalculationsInHoursOnly)
    {
        _ = column.Item().Text($"{BuildDuration75Title(settings, showTimeCalculationsInHoursOnly)} per type").Bold();

        if (summaries.Count == 0)
        {
            _ = column.Item().Text("No data.").FontColor(Colors.Grey.Darken1);
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(BuildDuration75Title(settings, showTimeCalculationsInHoursOnly));
            });

            foreach (var summary in summaries)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(PdfPresentationFormatting.FormatWorkDurationValue(
                        summary.DurationP75,
                        showTimeCalculationsInHoursOnly));
            }
        });
    }

    private static string BuildDurationColumnLabel(
        CustomTransitionAnalysisSettings settings,
        bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly
            ? $"Hours for \"{settings.Label}\""
            : $"Days for \"{settings.Label}\"";

    private static string BuildDuration75Title(
        CustomTransitionAnalysisSettings settings,
        bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly
            ? $"Hours for \"{settings.Label}\" 75P"
            : $"Days for \"{settings.Label}\" 75P";
}
