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
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        var settings = reportData.Settings.CustomTransitionAnalysis;
        if (settings is null)
        {
            return;
        }

        var issues = FindMatchingIssues(reportData, settings);

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
            issues,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);
    }

    private static IReadOnlyList<IssueTimeline> FindMatchingIssues(
        JiraPdfReportData reportData,
        CustomTransitionAnalysisSettings settings) =>
        [.. reportData.DoneIssues
            .Concat(reportData.RejectedIssues)
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Where(issue => issue.TryGetLastTransitionDuration(settings.FromStatusName, settings.ToStatusName).HasValue)
            .Where(issue => !settings.CodeOnly || issue.HasPullRequest)
            .OrderByDescending(issue => issue.TryGetLastTransitionDuration(settings.FromStatusName, settings.ToStatusName)!.Value)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    private static void ComposeIssueTable(
        ColumnDescriptor column,
        JiraPdfReportData reportData,
        CustomTransitionAnalysisSettings settings,
        IReadOnlyList<IssueTimeline> issues)
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
                var issue = issues[i];
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
                    .Text(BuildTransitionAtText(issue, settings));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(BuildTransitionDurationText(
                        issue,
                        settings,
                        reportData.Settings.ShowTimeCalculationsInHoursOnly));
            }
        });
    }

    private static void ComposeTransitionP75PerTypeSection(
        ColumnDescriptor column,
        CustomTransitionAnalysisSettings settings,
        IReadOnlyList<IssueTimeline> issues,
        bool showTimeCalculationsInHoursOnly)
    {
        _ = column.Item().Text($"{BuildDuration75Title(settings, showTimeCalculationsInHoursOnly)} per type").Bold();

        var summaries = BuildTransitionP75PerType(settings, issues);
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
                        summary.DaysAtWorkP75,
                        showTimeCalculationsInHoursOnly));
            }
        });
    }

    private static IReadOnlyList<IssueTypeWorkDays75Summary> BuildTransitionP75PerType(
        CustomTransitionAnalysisSettings settings,
        IReadOnlyList<IssueTimeline> issues) =>
        [.. issues
            .Select(issue => (issue.IssueType, duration: issue.TryGetLastTransitionDuration(settings.FromStatusName, settings.ToStatusName)))
            .Where(static sample => sample.duration.HasValue)
            .GroupBy(static sample => sample.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(static group =>
            {
                var issueType = group.First().IssueType;
                var samples = group.Select(static sample => sample.duration!.Value).ToList();

                return new IssueTypeWorkDays75Summary(
                    issueType,
                    new ItemCount(samples.Count),
                    CalculatePercentile(samples, 0.75));
            })
            .OrderByDescending(static summary => summary.DaysAtWorkP75)
            .ThenByDescending(static summary => summary.IssueCount.Value)
            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)];

    private static TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> values, double percentile)
    {
        var sorted = values
            .Select(static value => Math.Max(0.0, value.TotalSeconds))
            .OrderBy(static value => value)
            .ToList();
        if (sorted.Count == 0)
        {
            return TimeSpan.Zero;
        }

        if (sorted.Count == 1)
        {
            return TimeSpan.FromSeconds(sorted[0]);
        }

        var rank = (sorted.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        var fraction = rank - lowerIndex;
        return TimeSpan.FromSeconds(sorted[lowerIndex] + ((sorted[upperIndex] - sorted[lowerIndex]) * fraction));
    }

    private static string BuildTransitionAtText(IssueTimeline issue, CustomTransitionAnalysisSettings settings)
    {
        var transitionAt = issue.TryGetLastTransitionAt(settings.FromStatusName, settings.ToStatusName);
        return transitionAt.HasValue
            ? transitionAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";
    }

    private static string BuildTransitionDurationText(
        IssueTimeline issue,
        CustomTransitionAnalysisSettings settings,
        bool showTimeCalculationsInHoursOnly)
    {
        var duration = issue.TryGetLastTransitionDuration(settings.FromStatusName, settings.ToStatusName);
        return duration.HasValue
            ? PdfPresentationFormatting.FormatWorkDurationValue(duration.Value, showTimeCalculationsInHoursOnly)
            : "-";
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
