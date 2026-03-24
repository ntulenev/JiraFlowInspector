using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Preloaded report data required before detailed issue analysis starts.
/// </summary>
public sealed record JiraReportContext(
    IReadOnlyList<IssueKey> IssueKeys,
    IReadOnlyList<IssueKey> RejectIssueKeys,
    IReadOnlyList<ReleaseIssueItem> ReleaseIssues,
    IReadOnlyList<GlobalIncidentItem> GlobalIncidents,
    IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus);
