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
    /// <param name="primaryFieldName">Primary Jira date field name.</param>
    /// <param name="fallbackFieldName">Optional fallback Jira date field name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved primary field followed by the fallback field when configured.</returns>
    Task<IReadOnlyList<ResolvedJiraField>> ResolveDateFieldsAsync(
        JiraFieldName primaryFieldName,
        JiraFieldName? fallbackFieldName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a required field id.
    /// </summary>
    /// <param name="fieldName">Jira field name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved Jira field id.</returns>
    Task<JiraFieldId> ResolveFieldIdAsync(JiraFieldName fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an optional field id.
    /// </summary>
    /// <param name="fieldName">Optional Jira field name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved field id, or <see langword="null"/> when no field is configured.</returns>
    Task<JiraFieldId?> TryResolveFieldIdAsync(JiraFieldName? fieldName, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves hot-fix rules into field ids and normalized values.
    /// </summary>
    /// <param name="hotFixRules">Configured hot-fix rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rules with resolved Jira field ids.</returns>
    Task<IReadOnlyList<ResolvedHotFixRule>> ResolveHotFixRulesAsync(
        IReadOnlyList<HotFixRule> hotFixRules,
        CancellationToken cancellationToken);
}

