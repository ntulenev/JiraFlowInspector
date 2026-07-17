using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders issue timeline and duration-percentile HTML sections.
/// </summary>
internal sealed class HtmlIssueTimelineSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildIssueTimelineTable(
            "done-issues",
            "Issues moved to Done in selected period",
            reportData.Transitions.DoneIssues,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData));
        _ = html.Append(HtmlContentComposer.BuildDuration75PerTypeTable(
            "done-duration-75",
            $"{PresentationFormatting.GetWorkDuration75Title(reportData.Settings.ShowTimeCalculationsInHoursOnly)} per type",
            reportData.Transitions.DoneDaysAtWork75PerType,
            reportData.Settings.ShowTimeCalculationsInHoursOnly));

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            _ = html.Append(HtmlContentComposer.BuildIssueTimelineTable(
                "rejected-issues",
                "Issues moved to Rejected in selected period",
                reportData.Transitions.RejectedIssues,
                rejectStatusName,
                "Rejected At",
                reportData));
        }

        return html.ToString();
    }
}
