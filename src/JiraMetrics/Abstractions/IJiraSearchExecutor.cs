using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Executes Jira reads and paged searches.
/// </summary>
public interface IJiraSearchExecutor
{
    /// <summary>
    /// Loads the current Jira user.
    /// </summary>
    Task<JiraCurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads an issue including changelog.
    /// </summary>
    Task<JiraIssueResponse?> GetIssueWithChangelogAsync(IssueKey issueKey, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a paged search query.
    /// </summary>
    Task<IReadOnlyList<JiraIssueKeyResponse>> SearchIssuesAsync(
        string jql,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken);
}
