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
    Task<JiraIssueResponse?> GetIssueWithChangelogAsync(
        IssueKey issueKey,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads multiple issues in a single bulk request.
    /// </summary>
    Task<IReadOnlyList<JiraIssueResponse>> GetIssuesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads multiple issue changelogs keyed by Jira issue id.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<JiraHistoryResponse>>> GetIssueChangelogsAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a paged search query.
    /// </summary>
    Task<IReadOnlyList<JiraIssueKeyResponse>> SearchIssuesAsync(
        string jql,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken);
}
