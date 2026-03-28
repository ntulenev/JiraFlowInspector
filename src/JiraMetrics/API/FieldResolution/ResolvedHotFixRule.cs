using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.FieldResolution;

/// <summary>
/// Represents a hot-fix marker rule with an optional resolved field id.
/// </summary>
/// <param name="FieldName">Configured Jira field name.</param>
/// <param name="FieldId">Resolved Jira field id when the field exists.</param>
/// <param name="Values">Configured hot-fix marker values.</param>
public sealed record ResolvedHotFixRule(
    JiraFieldName FieldName,
    JiraFieldId? FieldId,
    IReadOnlySet<JiraFieldValue> Values);
