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
        var presentationData = QaTransitionPresentationData.Create(reportData);
        if (!presentationData.ShouldRender)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        _ = html.Append(BuildTableSection(
            "qa-summary",
            "QA Transition Analysis",
            "No QA transition data.",
            MetricColumns,
            [
                BuildTextMetricRow("Total Done Code Tasks", presentationData.DoneCodeIssueCount.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Total Rejected Code Tasks", presentationData.RejectedCodeIssueCount.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("Open Bugs", presentationData.OpenBugSummary),
                BuildTextMetricRow("Open On Prod", presentationData.OpenProdBugSummary),
                BuildTextMetricRow("Done Bugs", presentationData.DoneBugSummary),
                BuildTextMetricRow("Done On Prod", presentationData.DoneProdBugSummary),
                BuildTextMetricRow("Rejected Bugs", presentationData.RejectedBugSummary),
                BuildTextMetricRow("Rejected On Prod", presentationData.RejectedProdBugSummary),
                BuildTextMetricRow("QA In Progress Coverage", presentationData.PickupCoverageText),
                BuildTextMetricRow("QA In Progress 75P", presentationData.Pickup.Duration75Text),
                BuildTextMetricRow("QA On Hold Issues", presentationData.Hold.IssueCount.ToString(CultureInfo.InvariantCulture)),
                BuildTextMetricRow("QA Transition 75P", presentationData.Testing.Duration75Text),
                BuildTextMetricRow("QA Hold 75P", presentationData.Hold.Duration75Text)
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
                    BuildTextCell(presentationData.Pickup.RulesLabel),
                    BuildTextCell(presentationData.PickupIssueCountText),
                    BuildTextCell(presentationData.PickupShareText, presentationData.PickupIssuePercentage),
                    BuildTextCell(presentationData.Pickup.Duration75Text, presentationData.Pickup.Duration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true));

        _ = html.Append(BuildIssueTypeDuration75Table(
            "qa-pickup-75",
            "QA Pickup 75P per type",
            presentationData.Pickup.Duration75PerType,
            presentationData.Duration75ColumnLabel));
        _ = html.Append(BuildTransitionMeasurementTable(
            "qa-testing-issues",
            "Testing time by issue",
            presentationData.Testing.Issues,
            presentationData.DurationColumnLabel));
        _ = html.Append(BuildIssueTypeDuration75Table(
            "qa-testing-75",
            "Testing time 75P per type",
            presentationData.Testing.Duration75PerType,
            presentationData.Duration75ColumnLabel));
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
                    BuildTextCell(presentationData.Hold.RulesLabel),
                    BuildTextCell(presentationData.Hold.IssueCount.ToString(CultureInfo.InvariantCulture), presentationData.Hold.IssueCount),
                    BuildTextCell(presentationData.Hold.Duration75Text, presentationData.Hold.Duration75?.TotalMinutes)
                ])
            ],
            defaultSortColumn: 1,
            defaultSortDirection: "desc",
            compact: true));
        _ = html.Append(BuildTransitionMeasurementTable(
            "qa-hold-issues",
            "QA hold time by issue",
            presentationData.Hold.Issues,
            presentationData.HoldDurationColumnLabel));
        _ = html.Append(BuildIssueTypeDuration75Table(
            "qa-hold-75",
            "QA hold 75P per type",
            presentationData.Hold.Duration75PerType,
            presentationData.Duration75ColumnLabel));
        return html.ToString();
    }

    private static string BuildTransitionMeasurementTable(
        string sectionId,
        string title,
        IReadOnlyList<QaTransitionIssuePresentationData> issues,
        string durationColumnTitle)
    {
        var rows = issues
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildLinkCell(item.Key.Value, item.IssueUrl),
                BuildTextCell(item.IssueType.Value),
                BuildTextCell(item.SubItemsCount.ToString(CultureInfo.InvariantCulture), item.SubItemsCount),
                BuildTextCell(item.HasPullRequest ? "+" : string.Empty),
                BuildTextCell(item.Summary.Value),
                BuildTextCell(item.RuleLabel),
                BuildTextCell(item.TransitionAtText, item.TransitionAt.ToUnixTimeSeconds()),
                BuildTextCell(item.DurationText, item.Duration.TotalMinutes)
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
                new TableColumn(durationColumnTitle, "number", "Duration")
            ],
            rows,
            defaultSortColumn: 8,
            defaultSortDirection: "desc");
    }

    private static string BuildIssueTypeDuration75Table(
        string sectionId,
        string title,
        IReadOnlyList<QaDuration75PresentationData> summaries,
        string duration75Title)
    {
        var rows = summaries
            .Select(summary => new TableRow(
            [
                BuildTextCell(summary.IssueType.Value),
                BuildTextCell(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture), summary.IssueCount.Value),
                BuildTextCell(
                    summary.DurationText,
                    summary.Duration.TotalMinutes)
            ]))
            .ToList();

        return BuildTableSection(
            sectionId,
            title,
            "No data.",
            [
                new TableColumn("Type", "text", "Type"),
                new TableColumn("Issues", "number", "Issues"),
                new TableColumn(duration75Title, "number", "75P")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }

}
