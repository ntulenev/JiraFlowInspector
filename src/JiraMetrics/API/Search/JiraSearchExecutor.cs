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
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        return _transport.GetAsync<JiraIssueResponse>(
            new Uri(
                BuildIssueWithChangelogUrl(issueKey, fields),
                UriKind.Relative),
            cancellationToken);
    }

    public async Task<IReadOnlyList<JiraIssueResponse>> GetIssuesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<string>? fields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueKeys);

        if (issueKeys.Count == 0)
        {
            return [];
        }

        var response = await _transport.PostAsync<JiraBulkIssueFetchRequest, JiraBulkIssueFetchResponse>(
                new Uri("rest/api/3/issue/bulkfetch", UriKind.Relative),
                new JiraBulkIssueFetchRequest
                {
                    Fields = fields,
                    FieldsByKeys = false,
                    IssueIdsOrKeys = [.. issueKeys.Select(static key => key.Value)]
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira bulk issue response is empty.");
        }

        return response.Issues;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<JiraHistoryResponse>>> GetIssueChangelogsAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueKeys);

        if (issueKeys.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<JiraHistoryResponse>>(StringComparer.OrdinalIgnoreCase);
        }

        var historiesByIssueId = new Dictionary<string, List<JiraHistoryResponse>>(StringComparer.OrdinalIgnoreCase);
        string? nextPageToken = null;

        while (true)
        {
            var response = await _transport.PostAsync<JiraBulkChangelogFetchRequest, JiraBulkChangelogFetchResponse>(
                    new Uri("rest/api/3/changelog/bulkfetch", UriKind.Relative),
                    new JiraBulkChangelogFetchRequest
                    {
                        FieldIds = _bulkChangelogFieldIds,
                        IssueIdsOrKeys = [.. issueKeys.Select(static key => key.Value)],
                        MaxResults = BULK_CHANGELOG_PAGE_SIZE,
                        NextPageToken = nextPageToken
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
            {
                throw new InvalidOperationException("Jira bulk changelog response is empty.");
            }

            foreach (var issueChangeLog in response.IssueChangeLogs)
            {
                if (string.IsNullOrWhiteSpace(issueChangeLog.IssueId))
                {
                    continue;
                }

                if (!historiesByIssueId.TryGetValue(issueChangeLog.IssueId, out var histories))
                {
                    histories = [];
                    historiesByIssueId[issueChangeLog.IssueId] = histories;
                }

                histories.AddRange(issueChangeLog.ChangeHistories.Select(static history => history.ToHistoryResponse()));
            }

            nextPageToken = response.NextPageToken;
            if (string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return historiesByIssueId.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<JiraHistoryResponse>)[.. pair.Value],
            StringComparer.OrdinalIgnoreCase);
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

    private static string BuildIssueWithChangelogUrl(
        IssueKey issueKey,
        IReadOnlyList<string>? fields)
    {
        var issueUrl = $"rest/api/3/issue/{Uri.EscapeDataString(issueKey.Value)}?expand=changelog";
        if (fields is null)
        {
            return issueUrl;
        }

        var encodedFields = fields
            .Where(static field => !string.IsNullOrWhiteSpace(field))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(Uri.EscapeDataString)
            .ToArray();
        if (encodedFields.Length == 0)
        {
            return issueUrl;
        }

        return issueUrl + $"&fields={string.Join(",", encodedFields)}";
    }

    private readonly IJiraTransport _transport;
    private const int BULK_CHANGELOG_PAGE_SIZE = 1000;
    private static readonly string[] _bulkChangelogFieldIds = ["status"];
}
#pragma warning restore CS1591
