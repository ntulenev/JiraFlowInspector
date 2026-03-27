using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents transition analysis sections.
/// </summary>
public interface IJiraAnalysisPresenter
{
    void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName);

    void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName);

    void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName);

    void ShowPathGroupsSummary(PathGroupsSummary summary);

    void ShowPathGroups(IReadOnlyList<PathGroup> groups);

    void ShowSpacer();
}
