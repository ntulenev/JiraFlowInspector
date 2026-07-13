using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Preloaded report data required before detailed issue analysis starts.
/// </summary>
/// <param name="IssueKeys">The <paramref name="IssueKeys"/> value.</param>
/// <param name="RejectIssueKeys">The <paramref name="RejectIssueKeys"/> value.</param>
/// <param name="ReleaseIssues">The <paramref name="ReleaseIssues"/> value.</param>
/// <param name="ArchTasks">The <paramref name="ArchTasks"/> value.</param>
/// <param name="GlobalIncidents">The <paramref name="GlobalIncidents"/> value.</param>
/// <param name="Unresolved30DaysTasks">The <paramref name="Unresolved30DaysTasks"/> value.</param>
/// <param name="OpenIssuesByStatus">The <paramref name="OpenIssuesByStatus"/> value.</param>
/// <param name="RoadmapItems">The <paramref name="RoadmapItems"/> value.</param>
public sealed record JiraReportContext(
    IReadOnlyList<IssueKey> IssueKeys,
    IReadOnlyList<IssueKey> RejectIssueKeys,
    IReadOnlyList<ReleaseIssueItem> ReleaseIssues,
    IReadOnlyList<ArchTaskItem> ArchTasks,
    IReadOnlyList<GlobalIncidentItem> GlobalIncidents,
    IReadOnlyList<IssueListItem> Unresolved30DaysTasks,
    IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus,
    IReadOnlyList<RoadmapItem> RoadmapItems);
