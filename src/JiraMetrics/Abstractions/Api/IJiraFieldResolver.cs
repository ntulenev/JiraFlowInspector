using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.API.FieldResolution;

namespace JiraMetrics.Abstractions.Api;

/// <summary>
/// Resolves Jira field ids from configured names.
/// </summary>
public interface IJiraFieldResolver
{
    /// <summary>
    /// Resolves a primary date field and optional fallback field.
    /// </summary>
    Task<IReadOnlyList<ResolvedJiraField>> ResolveDateFieldsAsync(
        JiraFieldName primaryFieldName,
        JiraFieldName? fallbackFieldName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a required field id.
    /// </summary>
    Task<JiraFieldId> ResolveFieldIdAsync(JiraFieldName fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an optional field id.
    /// </summary>
    Task<JiraFieldId?> TryResolveFieldIdAsync(JiraFieldName? fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves hot-fix rules into field ids and normalized values.
    /// </summary>
    Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyList<HotFixRule> hotFixRules,
        CancellationToken cancellationToken);
}

