namespace JiraMetrics.Models;

/// <summary>
/// Carries the datasets loaded before report analysis and composition.
/// </summary>
/// <param name="ReportContext">Preloaded report context.</param>
/// <param name="AllTasksRatio">All-tasks ratio snapshot.</param>
/// <param name="BugRatio">Optional bug ratio snapshot.</param>
/// <param name="InternalIncidents">Optional internal-incident ratio snapshot.</param>
/// <param name="TestCoverage">Automated test coverage snapshot.</param>
internal sealed record JiraApplicationReportData(
    JiraReportContext ReportContext,
    IssueRatioSnapshot AllTasksRatio,
    IssueRatioSnapshot? BugRatio,
    IssueRatioSnapshot? InternalIncidents,
    TestCoverageSnapshot TestCoverage);
