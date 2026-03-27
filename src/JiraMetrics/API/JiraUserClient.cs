using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API;

internal sealed class JiraUserClient : IJiraUserClient
{
    private readonly IJiraSearchExecutor _searchExecutor;

    public JiraUserClient(IJiraSearchExecutor searchExecutor)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        _searchExecutor = searchExecutor;
    }

    public async Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await _searchExecutor
            .GetCurrentUserAsync(cancellationToken)
            .ConfigureAwait(false);
        if (response is null)
        {
            throw new InvalidOperationException("Jira user response is empty.");
        }

        var displayName =
            response.DisplayName
            ?? response.EmailAddress
            ?? response.AccountId
            ?? "unknown";
        return new JiraAuthUser(new UserDisplayName(displayName), response.EmailAddress, response.AccountId);
    }
}

