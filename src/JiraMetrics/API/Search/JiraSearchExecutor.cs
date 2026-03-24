using JiraMetrics.Abstractions;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

#pragma warning disable CS1591
namespace JiraMetrics.API.Search;

/// <summary>
/// Executes Jira reads that require paging or targeted resource fetches.
/// </summary>
public sealed class JiraSearchExecutor : IJiraSearchExecutor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraSearchExecutor"/> class.
    /// </summary>
    /// <param name="transport">Jira transport.</param>
    public JiraSearchExecutor(IJiraTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public Task<JiraCurrentUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _transport.GetAsync<JiraCurrentUserResponse>(
            new Uri("rest/api/3/myself", UriKind.Relative),
            cancellationToken);

    public Task<JiraIssueResponse?> GetIssueWithChangelogAsync(
        IssueKey issueKey,
        CancellationToken cancellationToken)
    {
        return _transport.GetAsync<JiraIssueResponse>(
            new Uri(
                $"rest/api/3/issue/{Uri.EscapeDataString(issueKey.Value)}?expand=changelog",
                UriKind.Relative),
            cancellationToken);
    }

    public async Task<IReadOnlyList<JiraIssueKeyResponse>> SearchIssuesAsync(
        string jql,
        IReadOnlyList<string> fields,
        CancellationToken cancellationToken)
    {
        var issues = new List<JiraIssueKeyResponse>();
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var searchUrl = BuildSearchUrl(jql, fields, pageSize, nextPageToken);
            var page = await _transport
                .GetAsync<JiraSearchResponse>(new Uri(searchUrl, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (page is null)
            {
                throw new InvalidOperationException("Jira search response is empty.");
            }

            if (page.Issues.Count > 0)
            {
                issues.AddRange(page.Issues);
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return issues;
    }

    private static string BuildSearchUrl(
        string jql,
        IReadOnlyList<string> fields,
        int pageSize,
        string? nextPageToken)
    {
        var encodedFields = fields
            .Where(static field => !string.IsNullOrWhiteSpace(field))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(Uri.EscapeDataString);
        var searchUrl =
            $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}"
            + $"&fields={string.Join(",", encodedFields)}&maxResults={pageSize}";

        if (!string.IsNullOrWhiteSpace(nextPageToken))
        {
            searchUrl += $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
        }

        return searchUrl;
    }

    private readonly IJiraTransport _transport;
}
#pragma warning restore CS1591
