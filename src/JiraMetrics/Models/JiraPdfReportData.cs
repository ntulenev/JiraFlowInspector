using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Aggregated data used by PDF report generation.
/// </summary>
public sealed class JiraPdfReportData
{
    /// <summary>
    /// Gets or sets application settings.
    /// </summary>
    public required AppSettings Settings { get; init; }

    /// <summary>
    /// Gets or sets count of issues returned by search query.
    /// </summary>
    public required ItemCount SearchIssueCount { get; init; }

    /// <summary>
    /// Gets or sets release issues for selected month.
    /// </summary>
    public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets bug issue count created in month.
    /// </summary>
    public ItemCount? BugCreatedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets bug issue count moved to done in month.
    /// </summary>
    public ItemCount? BugMovedToDoneThisMonth { get; init; }

    /// <summary>
    /// Gets or sets bug issue count moved to rejected in month.
    /// </summary>
    public ItemCount? BugRejectedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets finished bug issue count in month.
    /// </summary>
    public ItemCount? BugFinishedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets open bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugOpenIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets done bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugDoneIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets rejected bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugRejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets issues moved to done this month.
    /// </summary>
    public IReadOnlyList<IssueTimeline> DoneIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets issues moved to rejected this month.
    /// </summary>
    public IReadOnlyList<IssueTimeline> RejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets transition path summary.
    /// </summary>
    public required PathGroupsSummary PathSummary { get; init; }

    /// <summary>
    /// Gets or sets transition path groups.
    /// </summary>
    public IReadOnlyList<PathGroup> PathGroups { get; init; } = [];

    /// <summary>
    /// Gets or sets failed issue loads.
    /// </summary>
    public IReadOnlyList<LoadFailure> Failures { get; init; } = [];
}
