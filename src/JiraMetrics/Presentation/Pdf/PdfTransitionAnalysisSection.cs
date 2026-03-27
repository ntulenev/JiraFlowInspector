using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfTransitionAnalysisSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        _ = column.Item().Text("Transition analysis").Bold().FontSize(12);

        ComposeIssueTimelineSection(
            column,
            "Issues moved to Done in selected period",
            reportData.DoneIssues,
            reportData.Settings.BaseUrl,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData.Settings.ShowTimeCalculationsInHoursOnly,
            includeCreatedAt: true,
            includeDaysAtWork: true);
        ComposeDoneDaysAtWork75PerTypeSection(
            column,
            reportData.DoneDaysAtWork75PerType,
            reportData.Settings.DoneStatusName,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            ComposeIssueTimelineSection(
                column,
                "Issues moved to Rejected in selected period",
                reportData.RejectedIssues,
                reportData.Settings.BaseUrl,
                rejectStatusName,
                "Rejected At",
                reportData.Settings.ShowTimeCalculationsInHoursOnly,
                includeCreatedAt: true,
                includeDaysAtWork: true);
        }
    }

    private static void ComposeIssueTimelineSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueTimeline> issues,
        JiraBaseUrl baseUrl,
        StatusName targetStatusName,
        string atColumnTitle,
        bool showTimeCalculationsInHoursOnly,
        bool includeCreatedAt = false,
        bool includeDaysAtWork = false)
    {
        _ = column.Item().Text(title).Bold();

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
                if (includeCreatedAt)
                {
                    columns.ConstantColumn(82);
                }

                columns.ConstantColumn(90);
                if (includeDaysAtWork)
                {
                    columns.ConstantColumn(68);
                }
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Sub-items");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Code");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Summary");
                if (includeCreatedAt)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created At");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text(atColumnTitle);
                if (includeDaysAtWork)
                {
                    _ = header.Cell()
                        .Element(PdfPresentationHelpers.StyleHeaderCell)
                        .Text(PdfPresentationFormatting.GetWorkDurationColumnLabel(showTimeCalculationsInHoursOnly));
                }
            });

            for (var i = 0; i < orderedIssues.Length; i++)
            {
                var issue = orderedIssues[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(baseUrl, issue.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(issue.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.SubItemsCount.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.HasPullRequest ? "+" : string.Empty);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.Summary.Truncate(new TextLength(140)).Value);
                if (includeCreatedAt)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                }

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(PdfPresentationFormatting.BuildLastStatusAtText(issue, targetStatusName));
                if (includeDaysAtWork)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(PdfPresentationFormatting.BuildWorkDurationText(issue, targetStatusName, showTimeCalculationsInHoursOnly));
                }
            }
        });
    }

    private static void ComposeDoneDaysAtWork75PerTypeSection(
        ColumnDescriptor column,
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName,
        bool showTimeCalculationsInHoursOnly)
    {
        _ = column
            .Item()
            .Text($"{PdfPresentationFormatting.GetWorkDuration75Title(showTimeCalculationsInHoursOnly)} per type (moved to {doneStatusName.Value})")
            .Bold();
        if (summaries.Count == 0)
        {
            _ = column.Item().Text("No data.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedSummaries = summaries
            .OrderByDescending(static item => item.DaysAtWorkP75)
            .ThenByDescending(static item => item.IssueCount.Value)
            .ThenBy(static item => item.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
                    .Text(PdfPresentationFormatting.GetWorkDuration75Title(showTimeCalculationsInHoursOnly));
            });

            foreach (var summary in orderedSummaries)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(PdfPresentationFormatting.FormatWorkDurationValue(summary.DaysAtWorkP75, showTimeCalculationsInHoursOnly));
            }
        });
    }
}
