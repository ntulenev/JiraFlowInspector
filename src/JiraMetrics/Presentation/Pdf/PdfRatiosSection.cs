using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders ratio sections for all tasks and bug tasks in the PDF report.
/// </summary>
internal sealed class PdfRatiosSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraReportData reportData)
    {
        var presentationData = RatioSectionPresentationData.Create(reportData);
        ComposeAllTasksRatioSection(column, presentationData);
        ComposeBugRatioSection(column, presentationData, reportData.Settings.BaseUrl);
        ComposeInternalIncidentsSection(column, presentationData, reportData.Settings.BaseUrl);
    }

    private static void ComposeBugRatioSection(
        ColumnDescriptor column,
        RatioSectionPresentationData presentationData,
        JiraBaseUrl baseUrl)
    {
        if (presentationData.Bugs is not { } bugRatio)
        {
            return;
        }

        ComposeRatioSection(
            column,
            "Bug ratio",
            "Bug issue types",
            presentationData.BugIssueTypesLabel,
            bugRatio,
            includeReproducedOnProd: true);

        ComposeIssueListItemsSection(
            column,
            "Open issues",
            bugRatio.OpenIssues,
            PresentationFormatting.OPEN_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: true,
            includeReporducedOnProd: true);
        ComposeIssueListItemsSection(
            column,
            "Done issues",
            bugRatio.DoneIssues,
            PresentationFormatting.DONE_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: true,
            includeReporducedOnProd: true);
        ComposeIssueListItemsSection(
            column,
            "Rejected issues",
            bugRatio.RejectedIssues,
            PresentationFormatting.REJECTED_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: false,
            includeReporducedOnProd: true);
    }

    private static void ComposeAllTasksRatioSection(
        ColumnDescriptor column,
        RatioSectionPresentationData presentationData)
    {
        if (presentationData.AllTasks is not { } allTasksRatio)
        {
            return;
        }

        ComposeRatioSection(
            column,
            "All tasks ratio",
            "Issue types",
            "All",
            allTasksRatio,
            includeReproducedOnProd: false);
    }

    private static void ComposeInternalIncidentsSection(
        ColumnDescriptor column,
        RatioSectionPresentationData presentationData,
        JiraBaseUrl baseUrl)
    {
        if (presentationData.InternalIncidents is not { } internalIncidents)
        {
            return;
        }

        _ = column.Item().Text("Internal incidents").Bold().FontSize(12);
        _ = column.Item().Text($"Issue types: {presentationData.InternalIncidentIssueTypesLabel}")
            .FontColor(Colors.Grey.Darken1);

        ComposeIssueListItemsSection(
            column,
            "Open issues",
            internalIncidents.OpenIssues,
            PresentationFormatting.OPEN_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Done issues",
            internalIncidents.DoneIssues,
            PresentationFormatting.DONE_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Rejected issues",
            internalIncidents.RejectedIssues,
            PresentationFormatting.REJECTED_ISSUE_COLOR_HEX,
            baseUrl,
            includeCreationDate: false);
    }

    private static void ComposeRatioSection(
        ColumnDescriptor column,
        string title,
        string scopeLabel,
        string scopeValue,
        IssueRatioPresentationData ratio,
        bool includeReproducedOnProd)
    {
        _ = column.Item().Text(title).Bold().FontSize(12);

        column.Item().Table(table =>
        {
            void AddCountRow(string label, ItemCount count, ItemCount? prodCount = null)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(label);
                table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                {
                    _ = text.Span(count.Value.ToString(CultureInfo.InvariantCulture));
                    if (prodCount.HasValue)
                    {
                        _ = text.Span($" ({prodCount.Value.Value.ToString(CultureInfo.InvariantCulture)} on prod)")
                            .FontColor(Colors.Red.Medium);
                    }
                });
            }

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
            AddCountRow(
                "Open in selected period",
                ratio.OpenCount,
                includeReproducedOnProd ? ratio.OpenReproducedOnProdCount : null);
            AddCountRow(
                "Done in selected period",
                ratio.DoneCount,
                includeReproducedOnProd ? ratio.DoneReproducedOnProdCount : null);
            AddCountRow(
                "Rejected in selected period",
                ratio.RejectedCount,
                includeReproducedOnProd ? ratio.RejectedReproducedOnProdCount : null);
            AddCountRow(
                "Finished in selected period",
                ratio.FinishedCount,
                includeReproducedOnProd ? ratio.FinishedReproducedOnProdCount : null);
            if (includeReproducedOnProd)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Reproduced on prod");
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(
                    ratio.ReproducedOnProdCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished / Created");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(ratio.FinishedToCreatedRatioText);
        });
    }

    private static void ComposeIssueListItemsSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueListItem> issues,
        string titleColorHex,
        JiraBaseUrl baseUrl,
        bool includeCreationDate,
        bool includeReporducedOnProd = false)
    {
        _ = column.Item().Text(title).Bold().FontColor(titleColorHex);

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            ComposeIssueListTotals(column, issues, includeReporducedOnProd);
            return;
        }

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

                if (includeReporducedOnProd)
                {
                    columns.ConstantColumn(42);
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

                if (includeReporducedOnProd)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Prod");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
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
                        .Text(PresentationFormatting.FormatLocalDate(issue.CreatedAt));
                }

                if (includeReporducedOnProd)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(issue.ReporducedOnProd ? "Yes" : "No");
                }

                table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(text =>
                    {
                        _ = text.Span(issue.Title.Truncate(new TextLength(140)).Value).FontColor(titleColorHex);
                    });
            }
        });

        ComposeIssueListTotals(column, issues, includeReporducedOnProd);
    }

    private static void ComposeIssueListTotals(
        ColumnDescriptor column,
        IReadOnlyList<IssueListItem> issues,
        bool includeReporducedOnProd)
    {
        if (!includeReporducedOnProd)
        {
            return;
        }

        var prodCount = issues.Count(static issue => issue.ReporducedOnProd);
        _ = column.Item()
            .Text($"Total: {issues.Count.ToString(CultureInfo.InvariantCulture)}; Prod: {prodCount.ToString(CultureInfo.InvariantCulture)}")
            .Bold()
            .FontColor(Colors.Grey.Darken2);
    }
}
