using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents transition analysis sections.
/// </summary>
public interface IJiraAnalysisPresenter
{
    /// <summary>
    /// Shows issues moved to the done status.
    /// </summary>
    /// <param name="issues">Done issues.</param>
    /// <param name="doneStatusName">Done status.</param>
    void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName);

    /// <summary>
    /// Shows P75 work duration by issue type for done issues.
    /// </summary>
    /// <param name="summaries">P75 summaries.</param>
    /// <param name="doneStatusName">Done status.</param>
    void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName);

    /// <summary>
    /// Shows issues moved to the reject status.
    /// </summary>
    /// <param name="issues">Rejected issues.</param>
    /// <param name="rejectStatusName">Reject status.</param>
    void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName);

    /// <summary>
    /// Shows path group summary counters.
    /// </summary>
    /// <param name="summary">Path summary.</param>
    void ShowPathGroupsSummary(PathGroupsSummary summary);

    /// <summary>
    /// Shows detailed path groups.
    /// </summary>
    /// <param name="groups">Path groups.</param>
    void ShowPathGroups(IReadOnlyList<PathGroup> groups);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();
}
