using System.Globalization;
using System.Text;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the QA transition-analysis HTML section.
/// </summary>
internal sealed class HtmlQaTransitionAnalysisSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var analysis = reportData.Transitions.QaTransitionAnalysis;
        if (analysis.AnalyzedIssueCount.Value == 0)
        {
            return string.Empty;
        }

        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;
        var bugRatio = reportData.Ratios.Bugs;
        var html = new StringBuilder();
        _ = html.Append(BuildTableSection(
            "qa-summary",
            "QA Transition Analysis",
            "No QA transition data.",
            MetricColumns,
            [
                BuildTextMetricRow("Total Done Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.Transitions.DoneIssues).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Total Rejected Code Tasks", QaTransitionPresentationSummary.CountCodeIssues(reportData.Transitions.RejectedIssues).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Open Bugs", (bugRatio?.OpenIssues.Count ?? 0).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Open On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.OpenIssues ?? [])),
                BuildTextMetricRow("Done Bugs", (bugRatio?.DoneIssues.Count ?? 0).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Done On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.DoneIssues ?? [])),
                BuildTextMetricRow("Rejected Bugs", (bugRatio?.RejectedIssues.Count ?? 0).ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Rejected On Prod", QaTransitionPresentationSummary.BuildProdBugPrioritySummary(bugRatio?.RejectedIssues ?? [])),
                BuildTextMetricRow("QA In Progress Coverage", QaTransitionPresentationSummary.BuildCoverageText(analysis)),
                BuildTextMetricRow("QA In Progress 75P", QaTransitionPresentationSummary.FormatDuration(analysis.PickupDuration75, showHoursOnly)),
                BuildTextMetricRow("QA Transition 75P", QaTransitionPresentationSummary.FormatDuration(analysis.TestingDuration75, showHoursOnly)),
                BuildTextMetricRow("QA Hold 75P", QaTransitionPresentationSummary.FormatDuration(analysis.HoldDuration75, showHoursOnly))
            ],
            defaultSortColumn: 0,
            compact: true));

        _ = html.Append(BuildTableSection(
            "qa-pickup-summary",
            "QA Pickup",
            "No QA pickup data.",
            [
                new TableColumn("Transition", "text", "Transition"),
                new TableColumn("Issues", "text", "Issues"),
                new TableColumn("Share", "number", "Share"),
                new TableColumn("75P", "number", "75P")
            ],
            [
                new TableRow(
                [
                    BuildTextCell(QaTransitionPresentationSummary.BuildRulesLabel(reportData.Settings.QaTransitionAnalysis.PickupTransitions)),
                    BuildTextCell($"{analysis.PickupIssues.Count.ToString(CultureInfo.InvariantCulture)}/{analysis.AnalyzedIssueCount.Value.ToString(CultureInfo.InvariantCulture)}"),
                    BuildTextCell(analysis.PickupIssuePercentage.ToString("0.##", CultureInfo.InvariantCulture) + "%", analysis.PickupIssuePercentage),
                    BuildTextCell(QaTransitionPresentationSummary.FormatDuration(analysis.PickupDuration75, showHoursOnly), analysis.PickupDuration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true));

        _ = html.Append(BuildIssueTypeDuration75Table("qa-pickup-75", "QA Pickup 75P per type", analysis.PickupDuration75PerType, showHoursOnly));
        _ = html.Append(BuildTransitionMeasurementTable("qa-testing-issues", "Testing time by issue", analysis.TestingIssues, reportData));
        _ = html.Append(BuildIssueTypeDuration75Table("qa-testing-75", "Testing time 75P per type", analysis.TestingDuration75PerType, showHoursOnly));
        _ = html.Append(BuildTableSection(
            "qa-hold-summary",
            "QA Hold",
            "No QA hold data.",
            [
                new TableColumn("Transition", "text", "Transition"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn("75P", "number", "75P")
            ],
            [
                new TableRow(
                [
                    BuildTextCell(QaTransitionPresentationSummary.BuildRulesLabel(reportData.Settings.QaTransitionAnalysis.HoldTransitions)),
                    BuildTextCell(analysis.HoldIssues.Count.ToString(CultureInfo.InvariantCulture), analysis.HoldIssues.Count),
                    BuildTextCell(QaTransitionPresentationSummary.FormatDuration(analysis.HoldDuration75, showHoursOnly), analysis.HoldDuration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 1,
            defaultSortDirection: "desc",
            compact: true));
        _ = html.Append(BuildTransitionMeasurementTable(
            "qa-hold-issues",
            "QA hold time by issue",
            analysis.HoldIssues,
            reportData,
            QaTransitionPresentationSummary.GetHoldDurationColumnLabel(showHoursOnly)));
        _ = html.Append(BuildIssueTypeDuration75Table("qa-hold-75", "QA hold 75P per type", analysis.HoldDuration75PerType, showHoursOnly));
        return html.ToString();
    }

    private static string BuildTransitionMeasurementTable(
        string sectionId,
        string title,
        IReadOnlyList<TransitionMeasurementIssue> issues,
        JiraReportData reportData,
        string? durationColumnTitle = null)
    {
        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;
        var rows = issues
            .OrderByDescending(static item => item.Duration)
            .ThenBy(static item => item.Issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(item.Issue.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, item.Issue.Key)),
                BuildTextCell(item.Issue.IssueType.Value),
                BuildTextCell(item.Issue.SubItemsCount.ToString(CultureInfo.InvariantCulture), item.Issue.SubItemsCount),
                BuildTextCell(item.Issue.HasPullRequest ? "+" : string.Empty),
                BuildTextCell(item.Issue.Summary.Value),
                BuildTextCell(item.Rule.Label),
                BuildTextCell(HtmlPresentationHelpers.FormatDateTime(item.TransitionAt), item.TransitionAt.ToUnixTimeSeconds()),
                BuildTextCell(PresentationFormatting.FormatWorkDurationValue(item.Duration, showHoursOnly), item.Duration.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No issues.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Issue", "text", "Issue", "issue-column"),
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Sub-items", "number", "Sub-items"),
                new TableColumn("Code", "text", "Code"),
                new TableColumn("Summary", "text", "Summary", "summary-column"),
                new TableColumn("Measured transition", "text", "Measured transition"),
                new TableColumn("Transition At", "number", "Transition At"),
                new TableColumn(durationColumnTitle ?? QaTransitionPresentationSummary.GetDurationColumnLabel(showHoursOnly), "number", "Duration")
            ],
            rows,
            defaultSortColumn: 8,
            defaultSortDirection: "desc");
    }

    private static string BuildIssueTypeDuration75Table(
        string sectionId,
        string title,
        IReadOnlyList<IssueTypeDuration75Summary> summaries,
        bool showTimeCalculationsInHoursOnly)
    {
        var rows = summaries
            .OrderByDescending(static summary => summary.DurationP75)
            .ThenByDescending(static summary => summary.IssueCount.Value)
            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .Select(summary => new TableRow(
            [
                BuildTextCell(summary.IssueType.Value),
                BuildTextCell(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture), summary.IssueCount.Value),
                BuildTextCell(
                    PresentationFormatting.FormatWorkDurationValue(summary.DurationP75, showTimeCalculationsInHoursOnly),
                    summary.DurationP75.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No data.",
            [
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn(showTimeCalculationsInHoursOnly ? "Hours 75P" : "Days 75P", "number", "75P")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

}
