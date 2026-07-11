using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Loads Jira workflow data needed by the application.
/// </summary>
public interface IJiraApplicationDataFacade
{
    /// <summary>
    /// Loads the authenticated Jira user.
    /// </summary>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads pre-analysis report data.
    /// </summary>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    Task<JiraReportContext> LoadReportContextAsync(AppSettings settings, CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue-ratio data for a specific issue-type filter.
    /// </summary>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="issueTypes">The <paramref name="issueTypes"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    Task<IssueRatioSnapshot> LoadIssueRatioAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads automated test coverage data.
    /// </summary>
    /// <param name="settings">The <paramref name="settings"/> value.</param>
    /// <param name="coverageSettings">The <paramref name="coverageSettings"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    Task<TestCoverageSnapshot> LoadTestCoverageAsync(
        AppSettings settings,
        TestCoverageSettings coverageSettings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue timelines for the provided done and rejected issue keys.
    /// </summary>
    /// <param name="issueKeys">The <paramref name="issueKeys"/> value.</param>
    /// <param name="rejectIssueKeys">The <paramref name="rejectIssueKeys"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken);
}

