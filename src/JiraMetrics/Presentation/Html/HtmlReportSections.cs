using System.Text;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders the global-incidents HTML section.
/// </summary>
internal sealed class HtmlGlobalIncidentsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGlobalIncidentsTable(reportData);
}

/// <summary>
/// Renders issue-ratio and test-coverage HTML sections.
/// </summary>
internal sealed class HtmlRatiosSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildRatiosSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildBugRatioDetailsSection(reportData));
        _ = html.Append(HtmlContentComposer.BuildTestCoverageSection(reportData));
        return html.ToString();
    }
}

/// <summary>
/// Renders the QA transition-analysis HTML section.
/// </summary>
internal sealed class HtmlQaTransitionAnalysisSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildQaTransitionAnalysisSection(reportData);
}

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
            reportData.DoneIssues,
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData));
        _ = html.Append(HtmlContentComposer.BuildDuration75PerTypeTable(
            "done-duration-75",
            $"{PresentationFormatting.GetWorkDuration75Title(reportData.Settings.ShowTimeCalculationsInHoursOnly)} per type",
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

/// <summary>
/// Renders transition path summary and detail HTML sections.
/// </summary>
internal sealed class HtmlPathGroupsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildPathSummaryTable(reportData.PathSummary));
        _ = html.Append(HtmlContentComposer.BuildPathGroupsTable(reportData));
        return html.ToString();
    }
}

/// <summary>
/// Renders release and component-release HTML sections.
/// </summary>
internal sealed class HtmlReleaseSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(HtmlContentComposer.BuildReleaseTable(reportData));
        _ = html.Append(HtmlContentComposer.BuildComponentsReleaseTable(reportData));
        return html.ToString();
    }
}

/// <summary>
/// Renders the architecture-tasks HTML section.
/// </summary>
internal sealed class HtmlArchTasksSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildArchTasksTable(reportData);
}

/// <summary>
/// Renders the general-statistics HTML section.
/// </summary>
internal sealed class HtmlGeneralStatisticsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildGeneralStatisticsSection(reportData);
}

/// <summary>
/// Renders the issue-loading failures HTML section.
/// </summary>
internal sealed class HtmlFailuresSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData) =>
        HtmlContentComposer.BuildFailuresTable(reportData);
}
