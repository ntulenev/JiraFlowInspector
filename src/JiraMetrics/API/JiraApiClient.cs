using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.API;

/// <summary>
/// Jira REST API client implementation.
/// </summary>
public sealed class JiraApiClient : IJiraApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiClient"/> class.
    /// </summary>
    /// <param name="transport">Jira transport instance.</param>
    public JiraApiClient(IJiraTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        _transport = transport;
    }

    /// <inheritdoc />
    public async Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await _transport
            .GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira user response is empty.");
        }

        var displayName = response.DisplayName ?? response.EmailAddress ?? response.AccountId ?? "unknown";

        return new JiraAuthUser(new UserDisplayName(displayName), response.EmailAddress, response.AccountId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter,
        CancellationToken cancellationToken)
    {
        var escapedProject = EscapeJqlString(projectKey.Value);
        var escapedDoneStatus = EscapeJqlString(doneStatusName.Value);
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\"",
            $"status CHANGED TO \"{escapedDoneStatus}\" AFTER startOfMonth()"
        };

        if (createdAfter is { } createdAfterDate)
        {
            clauses.Add($"created >= \"{createdAfterDate}\"");
        }

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY key ASC";

        var issueKeys = new List<IssueKey>();
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var searchUrl = $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields=key&maxResults={pageSize}";
            if (!string.IsNullOrWhiteSpace(nextPageToken))
            {
                searchUrl += $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
            }

            var page = await _transport
                .GetAsync<JiraSearchResponse>(new Uri(searchUrl, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (page is null)
            {
                throw new InvalidOperationException("Jira search response is empty.");
            }

            if (page.Issues.Count > 0)
            {
                issueKeys.AddRange(page.Issues
                                       .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
                                       .Select(static issue => new IssueKey(issue.Key!.Trim())));
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return [.. issueKeys
            .DistinctBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    /// <inheritdoc />
    public async Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken)
    {
        var response = await _transport
            .GetAsync<JiraIssueResponse>(
                new Uri($"rest/api/3/issue/{Uri.EscapeDataString(issueKey.Value)}?expand=changelog", UriKind.Relative),
                cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira issue response is empty.");
        }

        if (response.Fields is null)
        {
            throw new InvalidOperationException("Response missing fields.");
        }

        if (string.IsNullOrWhiteSpace(response.Fields.Created)
            || !DateTimeOffset.TryParse(response.Fields.Created, out var created))
        {
            throw new InvalidOperationException("Issue created date is missing.");
        }

        var transitions = ParseTransitions(response.Changelog?.Histories ?? [], created);

        var endTime = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(response.Fields.ResolutionDate)
            && DateTimeOffset.TryParse(response.Fields.ResolutionDate, out var parsedResolutionDate))
        {
            endTime = parsedResolutionDate;
        }

        if (endTime < created)
        {
            endTime = created;
        }

        return new IssueTimeline(
            !string.IsNullOrWhiteSpace(response.Key) ? new IssueKey(response.Key.Trim()) : issueKey,
            IssueTypeName.FromNullable(response.Fields.IssueType?.Name),
            new IssueSummary(string.IsNullOrWhiteSpace(response.Fields.Summary) ? "No summary" : response.Fields.Summary),
            created,
            endTime,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }

    private static List<TransitionEvent> ParseTransitions(IReadOnlyList<JiraHistoryResponse> histories, DateTimeOffset created)
    {
        var rawTransitions = new List<(DateTimeOffset At, StatusName From, StatusName To)>();

        foreach (var history in histories)
        {
            if (string.IsNullOrWhiteSpace(history.Created)
                || !DateTimeOffset.TryParse(history.Created, out var at))
            {
                continue;
            }

            foreach (var item in history.Items.Where(static item =>
                         string.Equals(item.Field, "status", StringComparison.OrdinalIgnoreCase)))
            {
                rawTransitions.Add((
                    at,
                    StatusName.FromNullable(item.FromStatus),
                    StatusName.FromNullable(item.ToStatus)));
            }
        }

        rawTransitions = [.. rawTransitions.OrderBy(static item => item.At)];

        var transitions = new List<TransitionEvent>(rawTransitions.Count);
        var previousAt = created;

        foreach (var (At, From, To) in rawTransitions)
        {
            var at = At;
            if (at < created)
            {
                at = created;
            }

            if (at < previousAt)
            {
                at = previousAt;
            }

            var sincePrevious = at - previousAt;
            if (sincePrevious < TimeSpan.Zero)
            {
                sincePrevious = TimeSpan.Zero;
            }

            transitions.Add(new TransitionEvent(From, To, at, sincePrevious));
            previousAt = at;
        }

        return transitions;
    }

    private static string EscapeJqlString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private readonly IJiraTransport _transport;
}
