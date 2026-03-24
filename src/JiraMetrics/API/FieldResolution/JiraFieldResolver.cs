using JiraMetrics.Abstractions;
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
        string primaryFieldName,
        string? fallbackFieldName,
        CancellationToken cancellationToken)
    {
        var primaryFieldId = await ResolveFieldIdAsync(primaryFieldName, cancellationToken)
            .ConfigureAwait(false);
        var fields = new List<ResolvedJiraField>
        {
            new(primaryFieldName, primaryFieldId)
        };

        if (!string.IsNullOrWhiteSpace(fallbackFieldName))
        {
            var fallbackFieldId = await TryResolveFieldIdAsync(fallbackFieldName, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(fallbackFieldId)
                && !fields.Any(field => string.Equals(
                    field.FieldId,
                    fallbackFieldId,
                    StringComparison.OrdinalIgnoreCase)))
            {
                fields.Add(new ResolvedJiraField(fallbackFieldName, fallbackFieldId));
            }
        }

        return fields;
    }

    public async Task<string> ResolveFieldIdAsync(string fieldName, CancellationToken cancellationToken)
    {
        var fieldId = await TryResolveFieldIdAsync(fieldName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new InvalidOperationException($"Release date field '{fieldName}' was not found.");
        }

        return fieldId;
    }

    public async Task<string?> TryResolveFieldIdAsync(string? fieldName, CancellationToken cancellationToken)
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
            return preferredNormalized.Id!.Trim();
        }

        return null;
    }

    public async Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hotFixRules);

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
        foreach (var (fieldName, values) in normalizedRules.OrderBy(
            static pair => pair.Key,
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

    private static string NormalizeFieldName(string value) =>
        new([.. value
            .Where(static ch => char.IsLetterOrDigit(ch))
            .Select(static ch => char.ToLowerInvariant(ch))]);

    private static bool IsCustomFieldId(string fieldId) =>
        fieldId.StartsWith("customfield_", StringComparison.OrdinalIgnoreCase);

    private readonly IJiraTransport _transport;
    private IReadOnlyList<JiraFieldResponse>? _cachedFields;
}

public readonly record struct ResolvedJiraField(string FieldName, string FieldId);

public sealed record ResolvedHotFixRule(string FieldName, string? FieldId, HashSet<string> Values);
#pragma warning restore CS1591
