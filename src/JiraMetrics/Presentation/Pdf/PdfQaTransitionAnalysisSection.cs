using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
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

        var analysis = reportData.Transitions.QaTransitionAnalysis;
        if (analysis.AnalyzedIssueCount.Value == 0)
        {
            return;
        }

        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;

        _ = column.Item().Text("QA transition analysis").Bold().FontSize(12);
        ComposeQaSummary(column, reportData, analysis, showHoursOnly);
        ComposePickupSummary(
            column,
            reportData.Settings.QaTransitionAnalysis,
            analysis,
            showHoursOnly);
        ComposeDuration75PerTypeSection(
            column,
            "QA pickup 75P per type",
            analysis.PickupDuration75PerType,
            showHoursOnly);
        ComposeTestingSummary(
            column,
            reportData.Settings.QaTransitionAnalysis,
            analysis,
            showHoursOnly);
        ComposeIssueTable(
            column,
            "Testing time by issue",
            analysis.TestingIssues,
            reportData,
            showHoursOnly);
        ComposeDuration75PerTypeSection(
            column,
            "Testing time 75P per type",
            analysis.TestingDuration75PerType,
            showHoursOnly);
        ComposeHoldSummary(
            column,
            reportData.Settings.QaTransitionAnalysis,
            analysis,
            showHoursOnly);
        ComposeIssueTable(
            column,
            "QA hold time by issue",
            analysis.HoldIssues,
            reportData,
            showHoursOnly,
            QaTransitionPresentationSummary.GetHoldDurationColumnLabel(showHoursOnly));
        ComposeDuration75PerTypeSection(
            column,
            "QA hold 75P per type",
            analysis.HoldDuration75PerType,
            showHoursOnly);
    }

    private static void ComposeQaSummary(
        ColumnDescriptor column,
        JiraReportData reportData,
        QaTransitionAnalysis analysis,
        bool showTimeCalculationsInHoursOnly)
    {
        var bugRatio = reportData.Ratios.Bugs;
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

            AddSummaryRow(table, "Total Done Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.Transitions.DoneIssues));
            AddSummaryRow(table, "Total Rejected Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.Transitions.RejectedIssues));
            AddSummaryRow(table, "Open Bugs", bugRatio?.OpenIssues.Count ?? 0);
            AddSummaryRow(table, "Open On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.OpenIssues ?? []));
            AddSummaryRow(table, "Done Bugs", bugRatio?.DoneIssues.Count ?? 0);
            AddSummaryRow(table, "Done On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.DoneIssues ?? []));
            AddSummaryRow(table, "Rejected Bugs", bugRatio?.RejectedIssues.Count ?? 0);
            AddSummaryRow(table, "Rejected On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.RejectedIssues ?? []));
            AddSummaryRow(table, "QA In Progress Coverage", QaTransitionPresentationSummary.BuildCoverageText(analysis));
            AddSummaryRow(
                table,
                QaTransitionPresentationSummary.GetPickupDuration75Label(showTimeCalculationsInHoursOnly),
                QaTransitionPresentationSummary.FormatDuration(analysis.PickupDuration75, showTimeCalculationsInHoursOnly));
            AddSummaryRow(
                table,
                QaTransitionPresentationSummary.GetTestingDuration75Label(showTimeCalculationsInHoursOnly),
                QaTransitionPresentationSummary.FormatDuration(analysis.TestingDuration75, showTimeCalculationsInHoursOnly));
            AddSummaryRow(
                table,
                QaTransitionPresentationSummary.GetHoldDuration75Label(showTimeCalculationsInHoursOnly),
                QaTransitionPresentationSummary.FormatDuration(analysis.HoldDuration75, showTimeCalculationsInHoursOnly));
        });
    }

    private static void ComposePickupSummary(
        ColumnDescriptor column,
        QaTransitionAnalysisSettings settings,
        QaTransitionAnalysis analysis,
        bool showTimeCalculationsInHoursOnly)
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
                    .Text(QaTransitionPresentationSummary.GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.BuildRulesLabel(settings.PickupTransitions));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text($"{analysis.PickupIssues.Count}/{analysis.AnalyzedIssueCount.Value}");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(analysis.PickupIssuePercentage.ToString("0.##", CultureInfo.InvariantCulture) + "%");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.FormatDuration(analysis.PickupDuration75, showTimeCalculationsInHoursOnly));
        });
    }

    private static void ComposeTestingSummary(
        ColumnDescriptor column,
        QaTransitionAnalysisSettings settings,
        QaTransitionAnalysis analysis,
        bool showTimeCalculationsInHoursOnly)
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
                    .Text(QaTransitionPresentationSummary.GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.BuildRulesLabel(settings.TestingTransitions));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(analysis.TestingIssues.Count.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.FormatDuration(analysis.TestingDuration75, showTimeCalculationsInHoursOnly));
        });
    }

    private static void ComposeHoldSummary(
        ColumnDescriptor column,
        QaTransitionAnalysisSettings settings,
        QaTransitionAnalysis analysis,
        bool showTimeCalculationsInHoursOnly)
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
                    .Text(QaTransitionPresentationSummary.GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.BuildRulesLabel(settings.HoldTransitions));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(analysis.HoldIssues.Count.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.FormatDuration(analysis.HoldDuration75, showTimeCalculationsInHoursOnly));
        });
    }

    private static void ComposeIssueTable(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<TransitionMeasurementIssue> issues,
        JiraReportData reportData,
        bool showTimeCalculationsInHoursOnly,
        string? durationColumnLabel = null)
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
                    .Text(durationColumnLabel ?? QaTransitionPresentationSummary.GetDurationColumnLabel(showTimeCalculationsInHoursOnly));
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
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(item.Rule.Label);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(item.TransitionAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(PresentationFormatting.FormatWorkDurationValue(
                        item.Duration,
                        showTimeCalculationsInHoursOnly));
            }
        });
    }

    private static void ComposeDuration75PerTypeSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueTypeDuration75Summary> summaries,
        bool showTimeCalculationsInHoursOnly)
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
                    .Text(QaTransitionPresentationSummary.GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            foreach (var summary in summaries)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(PresentationFormatting.FormatWorkDurationValue(
                        summary.DurationP75,
                        showTimeCalculationsInHoursOnly));
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
