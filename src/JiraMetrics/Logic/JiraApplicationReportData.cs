using JiraMetrics.Models;

namespace JiraMetrics.Logic;

internal sealed record JiraApplicationReportData(
    JiraReportContext ReportContext,
    IssueRatioSnapshot AllTasksRatio,
    IssueRatioSnapshot? BugRatio);
