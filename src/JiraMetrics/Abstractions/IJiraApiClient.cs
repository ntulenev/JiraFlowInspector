using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Jira API client for retrieving issue and user data.
/// </summary>
public interface IJiraApiClient
{
    /// <summary>
    /// Loads authenticated Jira user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated user information.</returns>
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads issue keys moved to done this month.
    /// </summary>
    /// <param name="projectKey">Project key.</param>
    /// <param name="doneStatusName">Done status.</param>
    /// <param name="createdAfter">Optional lower bound for issue creation date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue keys.</returns>
    Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads full issue timeline for a specific issue key.
    /// </summary>
    /// <param name="issueKey">Issue key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue timeline.</returns>
    Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken);
}
