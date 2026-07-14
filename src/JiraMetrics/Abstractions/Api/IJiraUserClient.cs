using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Api;

/// <summary>
/// Loads information about the authenticated Jira user.
/// </summary>
internal interface IJiraUserClient
{
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);
}

