using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Contains the source datasets loaded before report analysis starts.
/// </summary>
public sealed class JiraReportSourceData
{
    /// <summary>
    /// Gets the count of issues returned by the main search query.
    /// </summary>
    public ItemCount SearchIssueCount { get; init; } = new(0);

    /// <summary>
    /// Gets release issues for the selected period.
    /// </summary>
    public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; init; } = [];

    /// <summary>
    /// Gets architecture tasks for the selected report query.
    /// </summary>
    public IReadOnlyList<ArchTaskItem> ArchTasks { get; init; } = [];

    /// <summary>
    /// Gets incidents for the selected period.
    /// </summary>
    public IReadOnlyList<GlobalIncidentItem> GlobalIncidents { get; init; } = [];

    /// <summary>
    /// Gets unresolved tasks older than 30 days as of report generation.
    /// </summary>
    public IReadOnlyList<IssueListItem> Unresolved30DaysTasks { get; init; } = [];

    /// <summary>
    /// Gets roadmap issues as they existed when the report was generated.
    /// </summary>
    public IReadOnlyList<RoadmapItem> RoadmapItems { get; init; } = [];

    /// <summary>
    /// Gets issue counts grouped by status and issue type outside done and rejected statuses.
    /// </summary>
    public IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus { get; init; } = [];
}
