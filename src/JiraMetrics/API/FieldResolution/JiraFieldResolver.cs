using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

#pragma warning disable CS1591
namespace JiraMetrics.API.FieldResolution;

/// <summary>
/// Resolves Jira field ids from configured names.
/// </summary>
public sealed class JiraFieldResolver : IJiraFieldResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraFieldResolver"/> class.
    /// </summary>
    /// <param name="transport">Jira transport.</param>
    public JiraFieldResolver(IJiraTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task<IReadOnlyList<ResolvedJiraField>> ResolveDateFieldsAsync(
        JiraFieldName primaryFieldName,
        JiraFieldName? fallbackFieldName,
        CancellationToken cancellationToken)
    {
        var primaryFieldId = await ResolveFieldIdAsync(primaryFieldName, cancellationToken)
            .ConfigureAwait(false);
        var fields = new List<ResolvedJiraField>
        {
            new(primaryFieldName, primaryFieldId)
        };

        if (fallbackFieldName is { } fallbackName)
        {
            var fallbackFieldId = await TryResolveFieldIdAsync(fallbackName, cancellationToken)
                .ConfigureAwait(false);
            if (fallbackFieldId is not null
                && !fields.Any(field => string.Equals(
                    field.FieldId.Value,
                    fallbackFieldId.Value.Value,
                    StringComparison.OrdinalIgnoreCase)))
            {
                fields.Add(new ResolvedJiraField(fallbackName, fallbackFieldId.Value));
            }
        }

        return fields;
    }

    public async Task<JiraFieldId> ResolveFieldIdAsync(JiraFieldName fieldName, CancellationToken cancellationToken)
    {
        var fieldId = await TryResolveFieldIdAsync(fieldName, cancellationToken).ConfigureAwait(false);
        if (fieldId is null)
        {
            throw new InvalidOperationException($"Release date field '{fieldName.Value}' was not found.");
        }

        return fieldId.Value;
    }

    public async Task<JiraFieldId?> TryResolveFieldIdAsync(
        JiraFieldName? fieldName,
        CancellationToken cancellationToken)
    {
        if (fieldName is null)
        {
            return null;
        }

        var trimmedFieldName = fieldName.Value.Value;
        lock (_cacheSync)
        {
            if (_resolvedFieldIds.TryGetValue(trimmedFieldName, out var cachedFieldId))
            {
                return cachedFieldId;
            }
        }

        var candidates = await GetFieldsAsync(cancellationToken).ConfigureAwait(false);
        var resolvedFieldId = ResolveFieldId(trimmedFieldName, candidates);

        lock (_cacheSync)
        {
            _resolvedFieldIds[trimmedFieldName] = resolvedFieldId;
        }

        return resolvedFieldId;
    }

    public async Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyList<HotFixRule> hotFixRules,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hotFixRules);

        var normalizedRules = new Dictionary<JiraFieldName, HashSet<JiraFieldValue>>();

        foreach (var rule in hotFixRules)
        {
            if (!normalizedRules.TryGetValue(rule.FieldName, out var values))
            {
                values = [];
                normalizedRules[rule.FieldName] = values;
            }

            values.UnionWith(rule.Values);
        }

        if (normalizedRules.Count == 0)
        {
            throw new InvalidOperationException("Hot-fix marker rules are empty.");
        }

        var resolvedRules = new List<ResolvedHotFixRule>(normalizedRules.Count);
        foreach (var (fieldName, values) in normalizedRules.OrderBy(
            static pair => pair.Key.Value,
            StringComparer.OrdinalIgnoreCase))
        {
            var fieldId = await TryResolveFieldIdAsync(fieldName, cancellationToken).ConfigureAwait(false);
            resolvedRules.Add(new ResolvedHotFixRule(fieldName, fieldId, values));
        }

        return resolvedRules;
    }

    private async Task<IReadOnlyList<JiraFieldResponse>> GetFieldsAsync(CancellationToken cancellationToken)
    {
        if (_cachedFields is not null)
        {
            return _cachedFields;
        }

        Task<IReadOnlyList<JiraFieldResponse>>? cachedFieldsTask;
        lock (_cacheSync)
        {
            cachedFieldsTask = _cachedFieldsTask;
            if (cachedFieldsTask is null)
            {
                cachedFieldsTask = LoadFieldsAsync(cancellationToken);
                _cachedFieldsTask = cachedFieldsTask;
            }
        }

        try
        {
            return await cachedFieldsTask.ConfigureAwait(false);
        }
        catch
        {
            lock (_cacheSync)
            {
                if (ReferenceEquals(_cachedFieldsTask, cachedFieldsTask))
                {
                    _cachedFieldsTask = null;
                }
            }

            throw;
        }
    }

    private async Task<IReadOnlyList<JiraFieldResponse>> LoadFieldsAsync(CancellationToken cancellationToken)
    {
        if (_cachedFields is not null)
        {
            return _cachedFields;
        }

        var response = await _transport
            .GetAsync<List<JiraFieldResponse>>(
                new Uri("rest/api/3/field", UriKind.Relative),
                cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("Jira fields response is empty.");
        }

        _cachedFields = [.. response.Where(static field => !string.IsNullOrWhiteSpace(field.Id))];
        return _cachedFields;
    }

    private static JiraFieldId? ResolveFieldId(
        string trimmedFieldName,
        IReadOnlyList<JiraFieldResponse> candidates)
    {
        var idMatch = candidates.FirstOrDefault(field =>
            string.Equals(field.Id!.Trim(), trimmedFieldName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(idMatch?.Id))
        {
            return new JiraFieldId(idMatch.Id.Trim());
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
            return new JiraFieldId(preferredExactName.Id!.Trim());
        }

        var normalizedTarget = NormalizeFieldName(trimmedFieldName);
        if (string.IsNullOrEmpty(normalizedTarget))
        {
            return null;
        }

        var normalizedMatches = candidates
            .Where(field =>
                !string.IsNullOrWhiteSpace(field.Name)
                && string.Equals(
                    NormalizeFieldName(field.Name),
                    normalizedTarget,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (normalizedMatches.Count > 0)
        {
            var preferredNormalized = normalizedMatches
                .OrderBy(static field => IsCustomFieldId(field.Id!) ? 1 : 0)
                .ThenBy(static field => field.Id, StringComparer.OrdinalIgnoreCase)
                .First();
            return new JiraFieldId(preferredNormalized.Id!.Trim());
        }

        return null;
    }

    private static string NormalizeFieldName(string value) =>
        new([.. value
            .Where(static ch => char.IsLetterOrDigit(ch))
            .Select(static ch => char.ToLowerInvariant(ch))]);

    private static bool IsCustomFieldId(string fieldId) =>
        fieldId.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase);

    private readonly IJiraTransport _transport;
    private readonly object _cacheSync = new();
    private IReadOnlyList<JiraFieldResponse>? _cachedFields;
    private Task<IReadOnlyList<JiraFieldResponse>>? _cachedFieldsTask;
    private readonly Dictionary<string, JiraFieldId?> _resolvedFieldIds =
        new(StringComparer.OrdinalIgnoreCase);
}

public readonly record struct ResolvedJiraField(JiraFieldName FieldName, JiraFieldId FieldId);

public sealed record ResolvedHotFixRule(
    JiraFieldName FieldName,
    JiraFieldId? FieldId,
    IReadOnlySet<JiraFieldValue> Values);
#pragma warning restore CS1591

