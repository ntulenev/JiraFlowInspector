using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.FieldResolution;

/// <summary>
/// Represents a Jira field name together with its resolved field id.
/// </summary>
/// <param name="FieldName">Configured Jira field name.</param>
/// <param name="FieldId">Resolved Jira field id.</param>
public readonly record struct ResolvedJiraField(JiraFieldName FieldName, JiraFieldId FieldId);
