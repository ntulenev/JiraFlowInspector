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

        var analysis = reportData.QaTransitionAnalysis;
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
            showHoursOnly ? "Hours on hold" : "Days on hold");
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

            AddSummaryRow(table, "Total Done Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.DoneIssues));
            AddSummaryRow(table, "Total Rejected Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.RejectedIssues));
            AddSummaryRow(table, "Open Bugs", reportData.BugOpenIssues.Count);
            AddSummaryRow(table, "Open On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugOpenIssues));
            AddSummaryRow(table, "Done Bugs", reportData.BugDoneIssues.Count);
            AddSummaryRow(table, "Done On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugDoneIssues));
            AddSummaryRow(table, "Rejected Bugs", reportData.BugRejectedIssues.Count);
            AddSummaryRow(table, "Rejected On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(reportData.BugRejectedIssues));
            AddSummaryRow(table, "QA In Progress Coverage", QaTransitionPresentationSummary.BuildCoverageText(analysis));
            AddSummaryRow(
                table,
                GetQaInProgressDuration75Label(showTimeCalculationsInHoursOnly),
                FormatDuration(analysis.PickupDuration75, showTimeCalculationsInHoursOnly));
            AddSummaryRow(
                table,
                GetQaTransitionDuration75Label(showTimeCalculationsInHoursOnly),
                FormatDuration(analysis.TestingDuration75, showTimeCalculationsInHoursOnly));
            AddSummaryRow(
                table,
                GetQaHoldDuration75Label(showTimeCalculationsInHoursOnly),
                FormatDuration(analysis.HoldDuration75, showTimeCalculationsInHoursOnly));
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
                    .Text(GetDuration75Title(showTimeCalculationsInHoursOnly));
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
                .Text(FormatDuration(analysis.PickupDuration75, showTimeCalculationsInHoursOnly));
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
                    .Text(GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.BuildRulesLabel(settings.TestingTransitions));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(analysis.TestingIssues.Count.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(FormatDuration(analysis.TestingDuration75, showTimeCalculationsInHoursOnly));
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
                    .Text(GetDuration75Title(showTimeCalculationsInHoursOnly));
            });

            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(QaTransitionPresentationSummary.BuildRulesLabel(settings.HoldTransitions));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(analysis.HoldIssues.Count.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(FormatDuration(analysis.HoldDuration75, showTimeCalculationsInHoursOnly));
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
                    .Text(durationColumnLabel ?? GetDurationColumnLabel(showTimeCalculationsInHoursOnly));
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
                    .Text(PdfPresentationFormatting.FormatWorkDurationValue(
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
                    .Text(GetDuration75Title(showTimeCalculationsInHoursOnly));
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

    private static string FormatDuration(TimeSpan? duration, bool showTimeCalculationsInHoursOnly) =>
        duration is null
            ? "-"
            : PdfPresentationFormatting.FormatWorkDurationValue(duration.Value, showTimeCalculationsInHoursOnly);

    private static void AddSummaryRow(TableDescriptor table, string metric, int value) =>
        AddSummaryRow(table, metric, value.ToString(CultureInfo.InvariantCulture));

    private static void AddSummaryRow(TableDescriptor table, string metric, string value)
    {
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(metric);
        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(value);
    }

    private static string GetDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA" : "Days in QA";

    private static string GetDuration75Title(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA 75P" : "Days in QA 75P";

    private static string GetQaInProgressDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA In Progress Hours 75p" : "QA In Progress Days 75p";

    private static string GetQaTransitionDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA Transition Hours 75p" : "QA Transition Days 75p";

    private static string GetQaHoldDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA Hold Hours 75p" : "QA Hold Days 75p";
}
