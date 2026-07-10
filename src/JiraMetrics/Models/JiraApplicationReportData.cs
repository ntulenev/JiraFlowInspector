namespace JiraMetrics.Models;

internal sealed record JiraApplicationReportData(
    JiraReportContext ReportContext,
    IssueRatioSnapshot AllTasksRatio,
    IssueRatioSnapshot? BugRatio,
    IssueRatioSnapshot? InternalIncidents,
    TestCoverageSnapshot TestCoverage);
