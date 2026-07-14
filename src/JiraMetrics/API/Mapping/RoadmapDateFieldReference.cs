using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Describes a Jira field containing a roadmap date.
/// </summary>
/// <param name="FieldId">Resolved Jira field id.</param>
/// <param name="JsonPropertyName">Nested interval property name, when applicable.</param>
public readonly record struct RoadmapDateFieldReference(
    JiraFieldId FieldId,
    string? JsonPropertyName);
