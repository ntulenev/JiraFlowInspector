using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Api;

internal interface IJiraUserClient
{
    Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken);
}

