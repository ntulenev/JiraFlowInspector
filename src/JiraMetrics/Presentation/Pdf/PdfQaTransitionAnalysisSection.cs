using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders QA-specific transition measurements.
/// </summary>
internal sealed class PdfQaTransitionAnalysisSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        var presentationData = QaTransitionPresentationData.Create(reportData);
        if (!presentationData.ShouldRender)
        {
            return;
        }

        _ = column.Item().Text("QA transition analysis").Bold().FontSize(12);
        ComposeQaSummary(column, presentationData);
        ComposePickupSummary(column, presentationData);
        ComposeDuration75PerTypeSection(
            column,
            "QA pickup 75P per type",
            presentationData.Pickup.Duration75PerType,
            presentationData.Duration75Title);
        ComposeTestingSummary(column, presentationData);
        ComposeIssueTable(
            column,
            "Testing time by issue",
            presentationData.Testing.Issues,
            presentationData.DurationColumnLabel);
        ComposeDuration75PerTypeSection(
            column,
            "Testing time 75P per type",
            presentationData.Testing.Duration75PerType,
            presentationData.Duration75Title);
        ComposeHoldSummary(column, presentationData);
        ComposeIssueTable(
            column,
            "QA hold time by issue",
            presentationData.Hold.Issues,
            presentationData.HoldDurationColumnLabel);
        ComposeDuration75PerTypeSection(
            column,
            "QA hold 75P per type",
            presentationData.Hold.Duration75PerType,
            presentationData.Duration75Title);
    }

    private static void ComposeQaSummary(
        ColumnDescriptor column,
        QaTransitionPresentationData presentationData)
    {
        _ = column.Item().Text("Summary").Bold();
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

            AddSummaryRow(table, "Total Done Code Tasks", presentationData.DoneCodeIssueCount);
            AddSummaryRow(table, "Total Rejected Code Tasks", presentationData.RejectedCodeIssueCount);
            AddSummaryRow(table, "Open Bugs", presentationData.OpenBugCount);
            AddSummaryRow(table, "Open On Prod", presentationData.OpenProdBugSummary);
            AddSummaryRow(table, "Done Bugs", presentationData.DoneBugCount);
            AddSummaryRow(table, "Done On Prod", presentationData.DoneProdBugSummary);
            AddSummaryRow(table, "Rejected Bugs", presentationData.RejectedBugCount);
            AddSummaryRow(table, "Rejected On Prod", presentationData.RejectedProdBugSummary);
            AddSummaryRow(table, "QA In Progress Coverage", presentationData.PickupCoverageText);
            AddSummaryRow(
                table,
                presentationData.PickupDuration75Label,
                presentationData.Pickup.Duration75Text);
            AddSummaryRow(
                table,
                presentationData.TestingDuration75Label,
                presentationData.Testing.Duration75Text);
            AddSummaryRow(
                table,
                presentationData.HoldDuration75Label,
                presentationData.Hold.Duration75Text);
        });
    }

    private static void ComposePickupSummary(
        ColumnDescriptor column,
        QaTransitionPresentationData presentationData)
    {
        _ = column.Item().Text("QA pickup").Bold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Transition");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Share");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(presentationData.Duration75Title);
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Pickup.RulesLabel);
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.PickupIssueCountText);
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.PickupShareText);
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Pickup.Duration75Text);
        });
    }

    private static void ComposeTestingSummary(
        ColumnDescriptor column,
        QaTransitionPresentationData presentationData)
    {
        _ = column.Item().Text("Testing time").Bold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Transitions");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(presentationData.Duration75Title);
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Testing.RulesLabel);
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Testing.IssueCount.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Testing.Duration75Text);
        });
    }

    private static void ComposeHoldSummary(
        ColumnDescriptor column,
        QaTransitionPresentationData presentationData)
    {
        _ = column.Item().Text("QA hold").Bold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Transitions");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(presentationData.Duration75Title);
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Hold.RulesLabel);
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Hold.IssueCount.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(presentationData.Hold.Duration75Text);
        });
    }

    private static void ComposeIssueTable(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<QaTransitionIssuePresentationData> issues,
        string durationColumnLabel)
    {
        _ = column.Item().Text(title).Bold();

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            return;
        }

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
                columns.ConstantColumn(110);
                columns.ConstantColumn(90);
                columns.ConstantColumn(82);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Sub-items");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Code");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Summary");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Measured transition");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Transition At");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(durationColumnLabel);
            });

            for (var i = 0; i < issues.Count; i++)
            {
                var item = issues[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(item.IssueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(item.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.SubItemsCount.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.HasPullRequest ? "+" : string.Empty);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.Summary.Truncate(new TextLength(140)).Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.RuleLabel);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(item.TransitionAtText);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(item.DurationText);
            }
        });
    }

    private static void ComposeDuration75PerTypeSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<QaDuration75PresentationData> summaries,
        string duration75Title)
    {
        _ = column.Item().Text(title).Bold();

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
                    .Text(duration75Title);
            });

            foreach (var summary in summaries)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(summary.DurationText);
            }
        });
    }

    private static void AddSummaryRow(TableDescriptor table, string metric, int value) =>
        AddSummaryRow(table, metric, value.ToString(CultureInfo.InvariantCulture));

    private static void AddSummaryRow(TableDescriptor table, string metric, string value)
    {
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(metric);
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(value);
    }

}
