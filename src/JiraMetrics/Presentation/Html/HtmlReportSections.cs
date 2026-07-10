using System.Text;

using JiraMetrics.Models;
using JiraMetrics.Presentation.Pdf;

namespace JiraMetrics.Presentation.Html;

internal sealed class HtmlGlobalIncidentsSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGlobalIncidentsTable(reportData);
}

internal sealed class HtmlRatiosSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildRatiosSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildBugRatioDetailsSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildTestCoverageSection(reportData));
        return html.ToString();
    }
}

internal sealed class HtmlQaTransitionAnalysisSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildQaTransitionAnalysisSection(reportData);
}

internal sealed class HtmlIssueTimelineSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildIssueTimelineTable(
            "done-issues",
            "Issues moved to Done in selected period",
            reportData.DoneIssues,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData));
        _ = html.Append(HtmlContentComposer.BuildDuration75PerTypeTable(
            "done-duration-75",
            $"{PdfPresentationFormatting.GetWorkDuration75Title(reportData.Settings.ShowTimeCalculationsInHoursOnly)} per type",
            reportData.DoneDaysAtWork75PerType,
            reportData.Settings.ShowTimeCalculationsInHoursOnly));

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            _ = html.Append(HtmlContentComposer.BuildIssueTimelineTable(
                "rejected-issues",
                "Issues moved to Rejected in selected period",
                reportData.RejectedIssues,
                rejectStatusName,
                "Rejected At",
                reportData));
        }

        return html.ToString();
    }
}

internal sealed class HtmlPathGroupsSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildPathSummaryTable(reportData.PathSummary));
        _ = html.Append(HtmlContentComposer.BuildPathGroupsTable(reportData));
        return html.ToString();
    }
}

internal sealed class HtmlReleaseSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildReleaseTable(reportData));
        _ = html.Append(HtmlContentComposer.BuildComponentsReleaseTable(reportData));
        return html.ToString();
    }
}

internal sealed class HtmlArchTasksSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildArchTasksTable(reportData);
}

internal sealed class HtmlGeneralStatisticsSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGeneralStatisticsSection(reportData);
}

internal sealed class HtmlFailuresSection : IHtmlReportSection
{
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildFailuresTable(reportData);
}
