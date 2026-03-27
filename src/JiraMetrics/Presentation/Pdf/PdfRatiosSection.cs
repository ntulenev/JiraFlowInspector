using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfRatiosSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        ComposeAllTasksRatioSection(column, reportData);
        ComposeBugRatioSection(column, reportData);
    }

    private static void ComposeBugRatioSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.BugCreatedThisMonth.HasValue
            || !reportData.BugMovedToDoneThisMonth.HasValue
            || !reportData.BugRejectedThisMonth.HasValue
            || !reportData.BugFinishedThisMonth.HasValue)
        {
            return;
        }

        var bugTypesLabel = reportData.Settings.BugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", reportData.Settings.BugIssueNames.Select(static x => x.Value));
        ComposeRatioSection(
            column,
            "Bug ratio",
            "Bug issue types",
            bugTypesLabel,
            new ItemCount(reportData.BugOpenIssues.Count),
            reportData.BugCreatedThisMonth.Value,
            reportData.BugMovedToDoneThisMonth.Value,
            reportData.BugRejectedThisMonth.Value,
            reportData.BugFinishedThisMonth.Value);

        ComposeIssueListItemsSection(
            column,
            "Open issues",
            reportData.BugOpenIssues,
            PdfPresentationFormatting.OPEN_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Done issues",
            reportData.BugDoneIssues,
            PdfPresentationFormatting.DONE_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Rejected issues",
            reportData.BugRejectedIssues,
            PdfPresentationFormatting.REJECTED_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: false);
    }

    private static void ComposeAllTasksRatioSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.AllTasksCreatedThisMonth.HasValue
            || !reportData.AllTasksOpenThisMonth.HasValue
            || !reportData.AllTasksMovedToDoneThisMonth.HasValue
            || !reportData.AllTasksRejectedThisMonth.HasValue
            || !reportData.AllTasksFinishedThisMonth.HasValue)
        {
            return;
        }

        ComposeRatioSection(
            column,
            "All tasks ratio",
            "Issue types",
            "All",
            reportData.AllTasksOpenThisMonth.Value,
            reportData.AllTasksCreatedThisMonth.Value,
            reportData.AllTasksMovedToDoneThisMonth.Value,
            reportData.AllTasksRejectedThisMonth.Value,
            reportData.AllTasksFinishedThisMonth.Value);
    }

    private static void ComposeRatioSection(
        ColumnDescriptor column,
        string title,
        string scopeLabel,
        string scopeValue,
        ItemCount openThisMonth,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        _ = column.Item().Text(title).Bold().FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Metric");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Value");
            });

            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(scopeLabel);
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(scopeValue);
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Open in selected period");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Done in selected period");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(movedToDoneThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Rejected in selected period");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(rejectedThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished in selected period");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(finishedThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished / Created");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(
                PdfPresentationFormatting.BuildFinishedToCreatedRatioText(createdThisMonth, finishedThisMonth));
        });
    }

    private static void ComposeIssueListItemsSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueListItem> issues,
        string titleColorHex,
        JiraBaseUrl baseUrl,
        bool includeCreationDate)
    {
        _ = column.Item().Text(title).Bold().FontColor(titleColorHex);

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
                columns.ConstantColumn(80);
                if (includeCreationDate)
                {
                    columns.ConstantColumn(82);
                }

                columns.RelativeColumn(5);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                if (includeCreationDate)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Creation Date");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
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
                if (includeCreationDate)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(issue.CreatedAt.HasValue
                            ? issue.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : "-");
                }

                table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(text =>
                    {
                        _ = text.Span(issue.Title.Truncate(new TextLength(140)).Value).FontColor(titleColorHex);
                    });
            }
        });
    }
}
