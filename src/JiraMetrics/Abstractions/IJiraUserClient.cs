using JiraMetrics.Models;

namespace JiraMetrics.Abstractions;

internal interface IJiraUserClient
{
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);
}
