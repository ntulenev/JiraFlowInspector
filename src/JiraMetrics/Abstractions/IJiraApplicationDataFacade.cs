using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Loads Jira workflow data needed by the application.
/// </summary>
public interface IJiraApplicationDataFacade
{
    /// <summary>
    /// Loads the authenticated Jira user.
    /// </summary>
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads pre-analysis report data.
    /// </summary>
    Task<JiraReportContext> LoadReportContextAsync(AppSettings settings, CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue-ratio data for a specific issue-type filter.
    /// </summary>
    Task<IssueRatioSnapshot> LoadIssueRatioAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue timelines for the provided done and rejected issue keys.
    /// </summary>
    Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken);
}
