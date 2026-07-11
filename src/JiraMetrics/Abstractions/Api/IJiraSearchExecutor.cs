using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api;

/// <summary>
/// Executes Jira reads and paged searches.
/// </summary>
public interface IJiraSearchExecutor
{
    /// <summary>
    /// Loads the current Jira user.
    /// </summary>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    Task<JiraCurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads an issue including changelog.
    /// </summary>
    /// <param name="issueKey">The <paramref name="issueKey"/> value.</param>
    /// <param name="fields">The <paramref name="fields"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    Task<JiraIssueResponse?> GetIssueWithChangelogAsync(
        IssueKey issueKey,
        JiraSearchFields? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads multiple issues in a single bulk request.
    /// </summary>
    /// <param name="issueKeys">The <paramref name="issueKeys"/> value.</param>
    /// <param name="fields">The <paramref name="fields"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    Task<IReadOnlyList<JiraIssueResponse>> GetIssuesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        JiraSearchFields? fields,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads multiple issue changelogs keyed by Jira issue id.
    /// </summary>
    /// <param name="issueKeys">The <paramref name="issueKeys"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<JiraHistoryResponse>>> GetIssueChangelogsAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a paged search query.
    /// </summary>
    /// <param name="jql">The <paramref name="jql"/> value.</param>
    /// <param name="fields">The <paramref name="fields"/> value.</param>
    /// <param name="cancellationToken">The <paramref name="cancellationToken"/> value.</param>
    /// <returns>The result of the operation.</returns>
    Task<IReadOnlyList<JiraIssueKeyResponse>> SearchIssuesAsync(
        JqlQuery jql,
        JiraSearchFields fields,
        CancellationToken cancellationToken);
}

