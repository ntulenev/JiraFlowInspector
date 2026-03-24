using JiraMetrics.API.FieldResolution;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Resolves Jira field ids from configured names.
/// </summary>
public interface IJiraFieldResolver
{
    /// <summary>
    /// Resolves a primary date field and optional fallback field.
    /// </summary>
    Task<IReadOnlyList<ResolvedJiraField>> ResolveDateFieldsAsync(
        string primaryFieldName,
        string? fallbackFieldName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a required field id.
    /// </summary>
    Task<string> ResolveFieldIdAsync(string fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an optional field id.
    /// </summary>
    Task<string?> TryResolveFieldIdAsync(string? fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves hot-fix rules into field ids and normalized values.
    /// </summary>
    Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        CancellationToken cancellationToken);
}
