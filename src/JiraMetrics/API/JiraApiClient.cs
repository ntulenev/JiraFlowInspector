using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using JiraMetrics.Abstractions;
using JiraMetrics.Helpers;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

namespace JiraMetrics.API;

/// <summary>
/// Jira REST API client implementation.
/// </summary>
public sealed partial class JiraApiClient : IJiraApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApiClient"/> class.
    /// </summary>
    /// <param name="transport">Jira transport instance.</param>
    /// <param name="settings">Application settings options.</param>
    /// <param name="transitionBuilder">Transition builder instance.</param>
    public JiraApiClient(
        IJiraTransport transport,
        IOptions<AppSettings> settings,
        ITransitionBuilder transitionBuilder)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(transitionBuilder);

        _transport = transport;
        _transitionBuilder = transitionBuilder;
        var resolved = settings.Value ?? throw new ArgumentException("App settings value is required.", nameof(settings));
        _customFieldName = string.IsNullOrWhiteSpace(resolved.CustomFieldName) ? null : resolved.CustomFieldName.Trim();
        _customFieldValue = string.IsNullOrWhiteSpace(resolved.CustomFieldValue) ? null : resolved.CustomFieldValue.Trim();
        _monthLabel = resolved.MonthLabel;
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
        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildMovedToDoneClause(doneStatusName, monthStart, nextMonthStart));

        if (createdAfter is { } createdAfterDate)
        {
            clauses.Add($"created >= \"{createdAfterDate}\"");
        }

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY key ASC";

        return await SearchIssueKeysAsync(jql, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ItemCount> GetIssueCountCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add($"created >= \"{monthStart:yyyy-MM-dd}\"");
        clauses.Add($"created < \"{nextMonthStart:yyyy-MM-dd}\"");
        AddIssueTypesClause(clauses, issueTypes);

        var count = await GetIssueCountByJqlAsync(string.Join(" AND ", clauses), cancellationToken).ConfigureAwait(false);
        return new ItemCount(count);
    }

    /// <inheritdoc />
    public async Task<ItemCount> GetIssueCountMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildMovedToDoneClause(doneStatusName, monthStart, nextMonthStart));
        AddIssueTypesClause(clauses, issueTypes);

        var count = await GetIssueCountByJqlAsync(string.Join(" AND ", clauses), cancellationToken).ConfigureAwait(false);
        return new ItemCount(count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
        ProjectKey projectKey,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add($"created >= \"{monthStart:yyyy-MM-dd}\"");
        clauses.Add($"created < \"{nextMonthStart:yyyy-MM-dd}\"");
        AddIssueTypesClause(clauses, issueTypes);

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY key ASC";
        return await SearchIssueListItemsAsync(jql, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildMovedToDoneClause(doneStatusName, monthStart, nextMonthStart));
        AddIssueTypesClause(clauses, issueTypes);

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY key ASC";
        return await SearchIssueListItemsAsync(jql, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var fieldId = await ResolveFieldIdAsync(releaseDateFieldName, cancellationToken).ConfigureAwait(false);
        var componentsFieldId = await TryResolveFieldIdAsync(componentsFieldName, cancellationToken).ConfigureAwait(false);
        var escapedProject = releaseProjectKey.Value.EscapeJqlString();
        var escapedLabel = projectLabel.EscapeJqlString();
        var escapedFieldName = releaseDateFieldName.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\"",
            $"labels = \"{escapedLabel}\"",
            $"\"{escapedFieldName}\" >= \"{monthStart:yyyy-MM-dd}\"",
            $"\"{escapedFieldName}\" < \"{nextMonthStart:yyyy-MM-dd}\""
        };

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY \"{escapedFieldName}\" ASC, key ASC";
        return await SearchReleaseIssueItemsAsync(
            jql,
            fieldId,
            releaseDateFieldName,
            componentsFieldId,
            componentsFieldName,
            cancellationToken).ConfigureAwait(false);
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
            PathLabel.FromTransitions(transitions),
            response.Fields.Subtasks.Count,
            HasPullRequest(response.Fields));
    }

    private IReadOnlyList<TransitionEvent> ParseTransitions(
        IReadOnlyList<JiraHistoryResponse> histories,
        DateTimeOffset created)
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

        return _transitionBuilder.BuildTransitions(rawTransitions, created);
    }

    private List<string> BuildProjectClauses(ProjectKey projectKey)
    {
        var escapedProject = projectKey.Value.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\""
        };

        if (!string.IsNullOrWhiteSpace(_customFieldName) && !string.IsNullOrWhiteSpace(_customFieldValue))
        {
            var escapedName = _customFieldName.EscapeJqlString();
            var escapedValue = _customFieldValue.EscapeJqlString();
            clauses.Add($"\"{escapedName}\" = \"{escapedValue}\"");
        }

        return clauses;
    }

    private static void AddIssueTypesClause(List<string> clauses, IReadOnlyList<IssueTypeName> issueTypes)
    {
        if (issueTypes.Count == 0)
        {
            return;
        }

        var escapedIssueTypes = issueTypes
            .Select(static issueType => $"\"{issueType.Value.EscapeJqlString()}\"")
            .ToArray();
        var issueTypeClause = escapedIssueTypes.Length == 1
            ? $"issuetype = {escapedIssueTypes[0]}"
            : $"issuetype IN ({string.Join(", ", escapedIssueTypes)})";

        clauses.Add(issueTypeClause);
    }

    private static string BuildMovedToDoneClause(StatusName doneStatusName, DateOnly monthStart, DateOnly nextMonthStart)
    {
        var escapedDoneStatus = doneStatusName.Value.EscapeJqlString();
        return $"status CHANGED TO \"{escapedDoneStatus}\" AFTER \"{monthStart:yyyy-MM-dd}\" BEFORE \"{nextMonthStart:yyyy-MM-dd}\" AND status = \"{escapedDoneStatus}\"";
    }

    private async Task<string> ResolveFieldIdAsync(string fieldName, CancellationToken cancellationToken)
    {
        var fieldId = await TryResolveFieldIdAsync(fieldName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new InvalidOperationException($"Release date field '{fieldName}' was not found.");
        }

        return fieldId;
    }

    private async Task<string?> TryResolveFieldIdAsync(string? fieldName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        var trimmedFieldName = fieldName.Trim();

        var response = await _transport
            .GetAsync<List<JiraFieldResponse>>(new Uri("rest/api/3/field", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira fields response is empty.");
        }

        var candidates = response
            .Where(static field => !string.IsNullOrWhiteSpace(field.Id))
            .ToList();

        var idMatch = candidates.FirstOrDefault(field =>
            string.Equals(field.Id!.Trim(), trimmedFieldName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(idMatch?.Id))
        {
            return idMatch.Id.Trim();
        }

        var exactNameMatches = candidates
            .Where(field =>
                !string.IsNullOrWhiteSpace(field.Name)
                && string.Equals(field.Name!.Trim(), trimmedFieldName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exactNameMatches.Count > 0)
        {
            var preferredExactName = exactNameMatches
                .OrderBy(static field => IsCustomFieldId(field.Id!) ? 1 : 0)
                .ThenBy(static field => field.Id, StringComparer.OrdinalIgnoreCase)
                .First();
            return preferredExactName.Id!.Trim();
        }

        var normalizedTarget = NormalizeFieldName(trimmedFieldName);
        if (string.IsNullOrEmpty(normalizedTarget))
        {
            return null;
        }

        var normalizedMatches = candidates
            .Where(field =>
                !string.IsNullOrWhiteSpace(field.Name)
                && string.Equals(NormalizeFieldName(field.Name), normalizedTarget, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (normalizedMatches.Count > 0)
        {
            var preferredNormalized = normalizedMatches
                .OrderBy(static field => IsCustomFieldId(field.Id!) ? 1 : 0)
                .ThenBy(static field => field.Id, StringComparer.OrdinalIgnoreCase)
                .First();
            return preferredNormalized.Id!.Trim();
        }

        return null;
    }

    private async Task<IReadOnlyList<IssueKey>> SearchIssueKeysAsync(string jql, CancellationToken cancellationToken)
    {
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

    private async Task<IReadOnlyList<IssueListItem>> SearchIssueListItemsAsync(string jql, CancellationToken cancellationToken)
    {
        var issues = new List<IssueListItem>();
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var searchUrl = $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields=key,summary,created&maxResults={pageSize}";
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
                issues.AddRange(page.Issues
                    .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
                    .Select(issue => new IssueListItem(
                        new IssueKey(issue.Key!.Trim()),
                        new IssueSummary(string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary),
                        ParseIssueCreatedAt(issue.Fields?.Created))));
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return [.. issues
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static DateTimeOffset? ParseIssueCreatedAt(string? rawCreated)
    {
        if (string.IsNullOrWhiteSpace(rawCreated))
        {
            return null;
        }

        return DateTimeOffset.TryParse(rawCreated, CultureInfo.InvariantCulture, DateTimeStyles.None, out var created)
            ? created
            : null;
    }

    private async Task<IReadOnlyList<ReleaseIssueItem>> SearchReleaseIssueItemsAsync(
        string jql,
        string releaseFieldId,
        string releaseDateFieldName,
        string? componentsFieldId,
        string? componentsFieldName,
        CancellationToken cancellationToken)
    {
        var issues = new List<ReleaseIssueItem>();
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var fields = new List<string>
            {
                "key",
                "summary",
                "issuelinks",
                Uri.EscapeDataString(releaseFieldId)
            };
            if (!string.IsNullOrWhiteSpace(componentsFieldId))
            {
                fields.Add(Uri.EscapeDataString(componentsFieldId));
            }
            if (!string.IsNullOrWhiteSpace(componentsFieldId) || !string.IsNullOrWhiteSpace(componentsFieldName))
            {
                const string standardComponentsFieldId = "components";
                if (!fields.Contains(standardComponentsFieldId, StringComparer.OrdinalIgnoreCase))
                {
                    fields.Add(standardComponentsFieldId);
                }
            }

            var searchUrl =
                $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields={string.Join(",", fields)}&maxResults={pageSize}";
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
                issues.AddRange(page.Issues
                    .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
                    .Select(issue =>
                    {
                        var releaseDate = TryParseReleaseDate(issue.Fields, releaseFieldId, releaseDateFieldName);
                        var tasks = CountCausedByLinkedTasks(issue.Fields);
                        var components = CountComponents(issue.Fields, componentsFieldId, componentsFieldName);
                        return (
                            key: new IssueKey(issue.Key!.Trim()),
                            title: new IssueSummary(string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary),
                            releaseDate,
                            tasks,
                            components);
                    })
                    .Where(static item => item.releaseDate.HasValue)
                    .Select(item => new ReleaseIssueItem(
                        item.key,
                        item.title,
                        item.releaseDate!.Value,
                        item.tasks,
                        item.components)));
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return [.. issues
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.ReleaseDate)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static DateOnly? TryParseReleaseDate(JiraIssueFieldsResponse? fields, string releaseFieldId, string releaseDateFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!TryGetAdditionalFieldValue(fields.AdditionalFields, releaseFieldId, releaseDateFieldName, out var rawDate))
        {
            return null;
        }

        if (rawDate.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (rawDate.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = rawDate.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            return DateOnly.FromDateTime(timestamp.UtcDateTime);
        }

        return null;
    }

    private static int CountComponents(
        JiraIssueFieldsResponse? fields,
        string? componentsFieldId,
        string? componentsFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return 0;
        }

        if (TryGetAdditionalFieldValue(fields.AdditionalFields, componentsFieldId, componentsFieldName, out var rawComponents))
        {
            var resolvedFieldCount = CountComponentsFromRaw(rawComponents);
            if (resolvedFieldCount > 0)
            {
                return resolvedFieldCount;
            }
        }

        if (fields.AdditionalFields.TryGetValue("components", out var standardComponents))
        {
            return CountComponentsFromRaw(standardComponents);
        }

        return 0;
    }

    private static int CountCausedByLinkedTasks(JiraIssueFieldsResponse? fields)
    {
        const string causedByRelation = "is caused by";

        if (fields?.IssueLinks.Count is not > 0)
        {
            return 0;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in fields.IssueLinks)
        {
            if (link?.Type is not { } linkType)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(linkType.Inward)
                && string.Equals(linkType.Inward.Trim(), causedByRelation, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(link.InwardIssue?.Key))
            {
                _ = keys.Add(link.InwardIssue.Key.Trim());
            }

            if (!string.IsNullOrWhiteSpace(linkType.Outward)
                && string.Equals(linkType.Outward.Trim(), causedByRelation, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(link.OutwardIssue?.Key))
            {
                _ = keys.Add(link.OutwardIssue.Key.Trim());
            }
        }

        return keys.Count;
    }

    private static bool HasPullRequest(JiraIssueFieldsResponse fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return false;
        }

        const string defaultDevelopmentFieldId = "customfield_10800";

        if (fields.AdditionalFields.TryGetValue(defaultDevelopmentFieldId, out var defaultDevelopmentField)
            && HasPullRequestInRawValue(defaultDevelopmentField))
        {
            return true;
        }

        foreach (var rawValue in fields.AdditionalFields.Values)
        {
            if (HasPullRequestInRawValue(rawValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPullRequestInRawValue(JsonElement rawValue)
    {
        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        var rawText = rawValue.ValueKind == JsonValueKind.String
            ? rawValue.GetString()
            : rawValue.GetRawText();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        if (rawText.IndexOf("pullrequest", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        var matches = PullRequestCountPattern().Matches(rawText);
        if (matches.Count == 0)
        {
            return true;
        }

        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            {
                continue;
            }

            if (count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetAdditionalFieldValue(
        Dictionary<string, JsonElement> additionalFields,
        string? fieldId,
        string? fieldName,
        out JsonElement value)
    {
        if (!string.IsNullOrWhiteSpace(fieldId) && additionalFields.TryGetValue(fieldId, out value))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fieldName) && additionalFields.TryGetValue(fieldName, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string? TryGetComponentValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (value.TryGetProperty("value", out var rawValue) && rawValue.ValueKind == JsonValueKind.String)
        {
            var text = rawValue.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        if (value.TryGetProperty("name", out var rawName) && rawName.ValueKind == JsonValueKind.String)
        {
            var text = rawName.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        if (value.TryGetProperty("id", out var rawId) && rawId.ValueKind == JsonValueKind.String)
        {
            var text = rawId.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return null;
    }

    private static string NormalizeFieldName(string value) =>
        new([.. value
            .Where(static ch => char.IsLetterOrDigit(ch))
            .Select(static ch => char.ToLowerInvariant(ch))]);

    private static bool IsCustomFieldId(string fieldId) =>
        fieldId.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase);

    private static int CountComponentsFromRaw(JsonElement rawComponents)
    {
        if (rawComponents.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return 0;
        }

        if (rawComponents.ValueKind == JsonValueKind.Array)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in rawComponents.EnumerateArray())
            {
                var value = TryGetComponentValue(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _ = values.Add(value);
                }
            }

            return values.Count;
        }

        if (rawComponents.ValueKind == JsonValueKind.String)
        {
            var raw = rawComponents.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static item => item.Trim())
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        if (rawComponents.ValueKind == JsonValueKind.Object)
        {
            return string.IsNullOrWhiteSpace(TryGetComponentValue(rawComponents)) ? 0 : 1;
        }

        return 0;
    }

    private async Task<int> GetIssueCountByJqlAsync(string jql, CancellationToken cancellationToken)
    {
        const int minAllowedPageSize = 1;
        var searchUrl = $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields=key&maxResults={minAllowedPageSize}";

        var page = await _transport
            .GetAsync<JiraSearchResponse>(new Uri(searchUrl, UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        if (page is null)
        {
            throw new InvalidOperationException("Jira search response is empty.");
        }

        return page.Total > 0 ? page.Total : page.Issues.Count;
    }

    private readonly IJiraTransport _transport;
    private readonly ITransitionBuilder _transitionBuilder;
    private readonly string? _customFieldName;
    private readonly string? _customFieldValue;
    private readonly MonthLabel _monthLabel;

    [GeneratedRegex(@"(?:stateCount|count)\s*""?\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestCountPattern();
}
