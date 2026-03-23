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
        _pullRequestFieldName = resolved.PullRequestFieldName;
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
    public async Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName,
        CancellationToken cancellationToken)
    {
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildExcludedStatusesClause(doneStatusName, rejectStatusName));

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY status ASC, key ASC";
        return await SearchIssueCountsByStatusAndTypeAsync(jql, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);
        ArgumentNullException.ThrowIfNull(hotFixRules);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackFieldName);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var fieldId = await ResolveFieldIdAsync(releaseDateFieldName, cancellationToken).ConfigureAwait(false);
        var componentsFieldId = await TryResolveFieldIdAsync(componentsFieldName, cancellationToken).ConfigureAwait(false);
        var resolvedHotFixRules = await ResolveHotFixRulesAsync(hotFixRules, cancellationToken).ConfigureAwait(false);
        var rollbackFieldId = await TryResolveFieldIdAsync(rollbackFieldName, cancellationToken).ConfigureAwait(false);
        var environmentFieldId = await TryResolveFieldIdAsync(environmentFieldName, cancellationToken).ConfigureAwait(false);
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
        if (!string.IsNullOrWhiteSpace(environmentFieldName) && !string.IsNullOrWhiteSpace(environmentFieldValue))
        {
            var escapedEnvironmentFieldName = environmentFieldName.EscapeJqlString();
            var escapedEnvironmentFieldValue = environmentFieldValue.EscapeJqlString();
            clauses.Add($"\"{escapedEnvironmentFieldName}\" = \"{escapedEnvironmentFieldValue}\"");
        }

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY \"{escapedFieldName}\" ASC, key ASC";
        return await SearchReleaseIssueItemsAsync(
            jql,
            fieldId,
            releaseDateFieldName,
            componentsFieldId,
            componentsFieldName,
            resolvedHotFixRules,
            rollbackFieldId,
            rollbackFieldName,
            environmentFieldId,
            environmentFieldName,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var startFieldId = await ResolveFieldIdAsync(settings.IncidentStartFieldName, cancellationToken).ConfigureAwait(false);
        var recoveryFieldId = await TryResolveFieldIdAsync(settings.IncidentRecoveryFieldName, cancellationToken).ConfigureAwait(false);
        var impactFieldId = await TryResolveFieldIdAsync(settings.ImpactFieldName, cancellationToken).ConfigureAwait(false);
        var urgencyFieldId = await TryResolveFieldIdAsync(settings.UrgencyFieldName, cancellationToken).ConfigureAwait(false);
        var additionalFieldIds = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var additionalFieldName in settings.AdditionalFieldNames)
        {
            additionalFieldIds[additionalFieldName] = await TryResolveFieldIdAsync(additionalFieldName, cancellationToken).ConfigureAwait(false);
        }

        var escapedNamespace = settings.Namespace.EscapeJqlString();
        var escapedStartFieldName = settings.IncidentStartFieldName.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedNamespace}\"",
            $"\"{escapedStartFieldName}\" >= \"{monthStart:yyyy-MM-dd}\"",
            $"\"{escapedStartFieldName}\" < \"{nextMonthStart:yyyy-MM-dd}\""
        };
        AddTextSearchClauses(clauses, settings.SearchPhrase);

        var jql = $"{string.Join(" AND ", clauses)} ORDER BY \"{escapedStartFieldName}\" ASC, key ASC";

        return await SearchGlobalIncidentItemsAsync(
            jql,
            startFieldId,
            settings.IncidentStartFieldName,
            recoveryFieldId,
            settings.IncidentRecoveryFieldName,
            impactFieldId,
            settings.ImpactFieldName,
            urgencyFieldId,
            settings.UrgencyFieldName,
            additionalFieldIds,
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
            HasPullRequest(response.Fields, _pullRequestFieldName));
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

    private static string BuildExcludedStatusesClause(StatusName doneStatusName, StatusName? rejectStatusName)
    {
        var statusesToExclude = new List<string>
        {
            $"\"{doneStatusName.Value.EscapeJqlString()}\""
        };

        if (rejectStatusName is { } rejectStatus
            && !string.Equals(doneStatusName.Value, rejectStatus.Value, StringComparison.OrdinalIgnoreCase))
        {
            statusesToExclude.Add($"\"{rejectStatus.Value.EscapeJqlString()}\"");
        }

        return statusesToExclude.Count == 1
            ? $"status != {statusesToExclude[0]}"
            : $"status NOT IN ({string.Join(", ", statusesToExclude)})";
    }

    private static void AddTextSearchClauses(List<string> clauses, string? searchPhrase)
    {
        if (string.IsNullOrWhiteSpace(searchPhrase))
        {
            return;
        }

        var terms = searchPhrase
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static term => term.Trim().Trim('"', '\''))
            .Where(static term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var term in terms)
        {
            var escapedTerm = term.EscapeJqlString();
            var pattern = escapedTerm.EndsWith('*') ? escapedTerm : escapedTerm + "*";
            clauses.Add($"text ~ \"{pattern}\"");
        }
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
        var candidates = await GetFieldsAsync(cancellationToken).ConfigureAwait(false);

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

    private async Task<IReadOnlyList<JiraFieldResponse>> GetFieldsAsync(CancellationToken cancellationToken)
    {
        if (_cachedFields is not null)
        {
            return _cachedFields;
        }

        var response = await _transport
            .GetAsync<List<JiraFieldResponse>>(new Uri("rest/api/3/field", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira fields response is empty.");
        }

        _cachedFields = [.. response.Where(static field => !string.IsNullOrWhiteSpace(field.Id))];
        return _cachedFields;
    }

    private async Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        CancellationToken cancellationToken)
    {
        var normalizedRules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rawFieldName, rawValues) in hotFixRules)
        {
            if (string.IsNullOrWhiteSpace(rawFieldName) || rawValues is null)
            {
                continue;
            }

            var fieldName = rawFieldName.Trim();
            if (!normalizedRules.TryGetValue(fieldName, out var values))
            {
                values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                normalizedRules[fieldName] = values;
            }

            foreach (var rawValue in rawValues)
            {
                if (!string.IsNullOrWhiteSpace(rawValue))
                {
                    _ = values.Add(rawValue.Trim());
                }
            }

            if (values.Count == 0)
            {
                _ = normalizedRules.Remove(fieldName);
            }
        }

        if (normalizedRules.Count == 0)
        {
            throw new InvalidOperationException("Hot-fix marker rules are empty.");
        }

        var resolvedRules = new List<ResolvedHotFixRule>(normalizedRules.Count);
        foreach (var (fieldName, values) in normalizedRules.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var fieldId = await TryResolveFieldIdAsync(fieldName, cancellationToken).ConfigureAwait(false);
            resolvedRules.Add(new ResolvedHotFixRule(fieldName, fieldId, values));
        }

        return resolvedRules;
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

    private async Task<IReadOnlyList<StatusIssueTypeSummary>> SearchIssueCountsByStatusAndTypeAsync(
        string jql,
        CancellationToken cancellationToken)
    {
        var countsByStatus = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var searchUrl =
                $"rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields=status,issuetype&maxResults={pageSize}";
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
                foreach (var issue in page.Issues)
                {
                    var statusName = StatusName.FromNullable(issue.Fields?.Status?.Name).Value;
                    var issueTypeName = IssueTypeName.FromNullable(issue.Fields?.IssueType?.Name).Value;

                    if (!countsByStatus.TryGetValue(statusName, out var issueTypeCounts))
                    {
#pragma warning disable IDE0028 // Simplify collection initialization
                        issueTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028 // Simplify collection initialization
                        countsByStatus[statusName] = issueTypeCounts;
                    }

                    issueTypeCounts[issueTypeName] = issueTypeCounts.TryGetValue(issueTypeName, out var count)
                        ? count + 1
                        : 1;
                }
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return [.. countsByStatus
            .Select(static statusGroup =>
            {
                var issueTypeSummaries = statusGroup.Value
                    .Select(static issueTypeGroup => new IssueTypeCountSummary(
                        IssueTypeName.FromNullable(issueTypeGroup.Key),
                        new ItemCount(issueTypeGroup.Value)))
                    .OrderByDescending(static summary => summary.Count.Value)
                    .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var totalCount = issueTypeSummaries.Sum(static summary => summary.Count.Value);

                return new StatusIssueTypeSummary(
                    StatusName.FromNullable(statusGroup.Key),
                    new ItemCount(totalCount),
                    issueTypeSummaries);
            })
            .OrderByDescending(static summary => summary.Count.Value)
            .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase)];
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

    private async Task<IReadOnlyList<GlobalIncidentItem>> SearchGlobalIncidentItemsAsync(
        string jql,
        string incidentStartFieldId,
        string incidentStartFieldName,
        string? incidentRecoveryFieldId,
        string incidentRecoveryFieldName,
        string? impactFieldId,
        string impactFieldName,
        string? urgencyFieldId,
        string urgencyFieldName,
        IReadOnlyDictionary<string, string?> additionalFieldIds,
        CancellationToken cancellationToken)
    {
        var incidents = new List<GlobalIncidentItem>();
        const int pageSize = 100;
        string? nextPageToken = null;

        while (true)
        {
            var fields = new List<string>
            {
                "key",
                "summary",
                Uri.EscapeDataString(incidentStartFieldId)
            };
            AddFieldIdIfNeeded(fields, incidentRecoveryFieldId);
            AddFieldIdIfNeeded(fields, impactFieldId);
            AddFieldIdIfNeeded(fields, urgencyFieldId);
            foreach (var additionalFieldId in additionalFieldIds.Values)
            {
                AddFieldIdIfNeeded(fields, additionalFieldId);
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
                incidents.AddRange(page.Issues
                    .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
                    .Select(issue =>
                    {
                        var startedAt = TryParseConfiguredDateTimeField(issue.Fields, incidentStartFieldId, incidentStartFieldName);
                        if (!startedAt.HasValue)
                        {
                            return null;
                        }

                        var recoveredAt = TryParseConfiguredDateTimeField(
                            issue.Fields,
                            incidentRecoveryFieldId,
                            incidentRecoveryFieldName);
                        var impact = ResolveFieldDisplayValue(issue.Fields, impactFieldId, impactFieldName);
                        var urgency = ResolveFieldDisplayValue(issue.Fields, urgencyFieldId, urgencyFieldName);
                        var additionalFields = ResolveAdditionalFieldValues(issue.Fields, additionalFieldIds);
                        return new GlobalIncidentItem(
                            new IssueKey(issue.Key!.Trim()),
                            new IssueSummary(string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary),
                            startedAt,
                            recoveredAt,
                            impact,
                            urgency,
                            additionalFields);
                    })
                    .Where(static item => item is not null)
                    .Cast<GlobalIncidentItem>());
            }

            nextPageToken = page.NextPageToken;
            if (page.Issues.Count == 0 || page.IsLast || string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return [.. incidents
            .DistinctBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private async Task<IReadOnlyList<ReleaseIssueItem>> SearchReleaseIssueItemsAsync(
        string jql,
        string releaseFieldId,
        string releaseDateFieldName,
        string? componentsFieldId,
        string? componentsFieldName,
        IReadOnlyList<ResolvedHotFixRule> hotFixRules,
        string? rollbackFieldId,
        string rollbackFieldName,
        string? environmentFieldId,
        string? environmentFieldName,
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
                "status",
                "issuelinks",
                Uri.EscapeDataString(releaseFieldId)
            };
            if (!string.IsNullOrWhiteSpace(componentsFieldId))
            {
                fields.Add(Uri.EscapeDataString(componentsFieldId));
            }
            foreach (var hotFixFieldId in hotFixRules
                .Select(static rule => rule.FieldId)
                .Where(static fieldId => !string.IsNullOrWhiteSpace(fieldId))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                fields.Add(Uri.EscapeDataString(hotFixFieldId!));
            }
            if (!string.IsNullOrWhiteSpace(rollbackFieldId))
            {
                fields.Add(Uri.EscapeDataString(rollbackFieldId));
            }
            if (!string.IsNullOrWhiteSpace(environmentFieldId))
            {
                fields.Add(Uri.EscapeDataString(environmentFieldId));
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
                        var status = StatusName.FromNullable(issue.Fields?.Status?.Name);
                        var tasks = CountAllLinkedTasks(issue.Fields);
                        var componentNames = ResolveComponentNames(issue.Fields, componentsFieldId, componentsFieldName);
                        var components = componentNames.Count;
                        var environmentNames = ResolveEnvironmentNames(issue.Fields, environmentFieldId, environmentFieldName);
                        var rollbackType = ResolveRollbackPayload(issue.Fields, rollbackFieldId, rollbackFieldName);
                        var isHotFix = IsHotFixRelease(issue.Fields, hotFixRules);
                        return (
                            key: new IssueKey(issue.Key!.Trim()),
                            title: new IssueSummary(string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary),
                            releaseDate,
                            status,
                            tasks,
                            components,
                            componentNames,
                            environmentNames,
                            rollbackType,
                            isHotFix);
                    })
                    .Where(static item => item.releaseDate.HasValue)
                    .Select(item => new ReleaseIssueItem(
                        item.key,
                        item.title,
                        item.releaseDate!.Value,
                        item.tasks,
                        item.components,
                        item.status,
                        item.componentNames,
                        item.environmentNames,
                        item.rollbackType,
                        item.isHotFix)));
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

    private static IReadOnlyList<string> ResolveComponentNames(
        JiraIssueFieldsResponse? fields,
        string? componentsFieldId,
        string? componentsFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return [];
        }

        if (TryGetAdditionalFieldValue(fields.AdditionalFields, componentsFieldId, componentsFieldName, out var rawComponents))
        {
            var resolvedValues = ParseComponentValues(rawComponents);
            if (resolvedValues.Count > 0)
            {
                return resolvedValues;
            }
        }

        if (fields.AdditionalFields.TryGetValue("components", out var standardComponents))
        {
            return ParseComponentValues(standardComponents);
        }

        return [];
    }

    private static IReadOnlyList<string> ResolveEnvironmentNames(
        JiraIssueFieldsResponse? fields,
        string? environmentFieldId,
        string? environmentFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return [];
        }

        if (!TryGetAdditionalFieldValue(fields.AdditionalFields, environmentFieldId, environmentFieldName, out var rawEnvironments))
        {
            return [];
        }

        return ParseRawFieldValues(rawEnvironments);
    }

    private static int CountAllLinkedTasks(JiraIssueFieldsResponse? fields)
    {
        if (fields?.IssueLinks.Count is not > 0)
        {
            return 0;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in fields.IssueLinks)
        {
            if (link is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(link.InwardIssue?.Key))
            {
                _ = keys.Add(link.InwardIssue.Key.Trim());
            }

            if (!string.IsNullOrWhiteSpace(link.OutwardIssue?.Key))
            {
                _ = keys.Add(link.OutwardIssue.Key.Trim());
            }
        }

        return keys.Count;
    }

    private static bool IsHotFixRelease(
        JiraIssueFieldsResponse? fields,
        IReadOnlyList<ResolvedHotFixRule> hotFixRules)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0 || hotFixRules.Count == 0)
        {
            return false;
        }

        foreach (var hotFixRule in hotFixRules)
        {
            if (!TryGetAdditionalFieldValue(fields.AdditionalFields, hotFixRule.FieldId, hotFixRule.FieldName, out var rawValue))
            {
                continue;
            }

            var matchesRule = ParseRawFieldValues(rawValue)
                .Any(hotFixRule.Values.Contains);
            if (matchesRule)
            {
                return true;
            }
        }

        return false;
    }

    private static string? ResolveRollbackPayload(
        JiraIssueFieldsResponse? fields,
        string? rollbackFieldId,
        string rollbackFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!TryGetAdditionalFieldValue(fields.AdditionalFields, rollbackFieldId, rollbackFieldName, out var rawRollback))
        {
            return null;
        }

        if (rawRollback.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var parsedValues = ParseRawFieldValues(rawRollback);
        if (parsedValues.Count > 0)
        {
            return string.Join(", ", parsedValues);
        }

        var rawPayload = rawRollback.ValueKind == JsonValueKind.String
            ? rawRollback.GetString()
            : rawRollback.GetRawText();
        return string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload.Trim();
    }

    private static bool HasPullRequest(JiraIssueFieldsResponse fields, string pullRequestFieldName)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(pullRequestFieldName)
            && fields.AdditionalFields.TryGetValue(pullRequestFieldName, out var configuredPullRequestField)
            && HasPullRequestInRawValue(configuredPullRequestField))
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

    private static void AddFieldIdIfNeeded(List<string> fields, string? fieldId)
    {
        if (!string.IsNullOrWhiteSpace(fieldId))
        {
            fields.Add(Uri.EscapeDataString(fieldId));
        }
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

    private static IReadOnlyList<string> ParseRawFieldValues(JsonElement rawValue)
    {
        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (rawValue.ValueKind == JsonValueKind.Array)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in rawValue.EnumerateArray())
            {
                var parsed = TryGetFieldValue(item);
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    _ = values.Add(parsed.Trim());
                }
            }

            return [.. values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
        }

        var resolved = TryGetFieldValue(rawValue);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return [];
        }

        return [resolved.Trim()];
    }

    private static string? TryGetFieldValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
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

            var adfText = TryExtractAtlassianDocumentText(value);
            if (!string.IsNullOrWhiteSpace(adfText))
            {
                return adfText;
            }
        }

        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetRawText();
        }

        return null;
    }

    private static string? TryExtractAtlassianDocumentText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!value.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || !string.Equals(typeElement.GetString(), "doc", StringComparison.OrdinalIgnoreCase)
            || !value.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var fragments = new List<string>();
        AppendAtlassianDocumentText(contentElement, fragments);
        var text = string.Join(" ", fragments.Where(static fragment => !string.IsNullOrWhiteSpace(fragment))).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void AppendAtlassianDocumentText(JsonElement value, List<string> fragments)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                AppendAtlassianDocumentText(item, fragments);
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (value.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
        {
            var text = textElement.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                fragments.Add(text.Trim());
            }
        }

        if (value.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Array)
        {
            AppendAtlassianDocumentText(contentElement, fragments);
        }
    }

    private static DateTimeOffset? TryParseConfiguredDateTimeField(
        JiraIssueFieldsResponse? fields,
        string? fieldId,
        string? fieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!TryGetAdditionalFieldValue(fields.AdditionalFields, fieldId, fieldName, out var rawDateTime))
        {
            return null;
        }

        var resolvedValues = ParseRawFieldValues(rawDateTime);
        if (resolvedValues.Count == 0 || string.IsNullOrWhiteSpace(resolvedValues[0]))
        {
            return null;
        }
        var resolvedValue = resolvedValues[0];

        if (DateTimeOffset.TryParseExact(
                resolvedValue,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var exactUtcDateTime))
        {
            return exactUtcDateTime;
        }

        if (DateTimeOffset.TryParse(
                resolvedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDateTime))
        {
            return parsedDateTime;
        }

        if (DateOnly.TryParse(resolvedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        return null;
    }

    private static string? ResolveFieldDisplayValue(
        JiraIssueFieldsResponse? fields,
        string? fieldId,
        string? fieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!TryGetAdditionalFieldValue(fields.AdditionalFields, fieldId, fieldName, out var rawValue))
        {
            return null;
        }

        var parsedValues = ParseRawFieldValues(rawValue);
        if (parsedValues.Count > 0)
        {
            return string.Join(", ", parsedValues);
        }

        var rawPayload = rawValue.ValueKind == JsonValueKind.String
            ? rawValue.GetString()
            : rawValue.GetRawText();
        return string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload.Trim();
    }

    private static Dictionary<string, string?> ResolveAdditionalFieldValues(
        JiraIssueFieldsResponse? fields,
        IReadOnlyDictionary<string, string?> additionalFieldIds)
    {
        if (additionalFieldIds.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (fieldName, fieldId) in additionalFieldIds)
        {
            values[fieldName] = ResolveFieldDisplayValue(fields, fieldId, fieldName);
        }

        return values;
    }

    private static string NormalizeFieldName(string value) =>
        new([.. value
            .Where(static ch => char.IsLetterOrDigit(ch))
            .Select(static ch => char.ToLowerInvariant(ch))]);

    private static bool IsCustomFieldId(string fieldId) =>
        fieldId.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> ParseComponentValues(JsonElement rawComponents)
    {
        if (rawComponents.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (rawComponents.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rawComponents.EnumerateArray())
            {
                var value = TryGetComponentValue(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _ = values.Add(value);
                }
            }

            return [.. values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
        }

        if (rawComponents.ValueKind == JsonValueKind.String)
        {
            var raw = rawComponents.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            return [.. raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static item => item.Trim())
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)];
        }

        if (rawComponents.ValueKind == JsonValueKind.Object)
        {
            var value = TryGetComponentValue(rawComponents);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = values.Add(value);
            }

            return [.. values.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)];
        }

        return [];
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

    private sealed record ResolvedHotFixRule(string FieldName, string? FieldId, HashSet<string> Values);

    private readonly IJiraTransport _transport;
    private readonly ITransitionBuilder _transitionBuilder;
    private readonly string? _customFieldName;
    private readonly string? _customFieldValue;
    private readonly MonthLabel _monthLabel;
    private readonly string _pullRequestFieldName;
    private IReadOnlyList<JiraFieldResponse>? _cachedFields;

    [GeneratedRegex(@"(?:stateCount|count)\s*""?\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestCountPattern();
}
