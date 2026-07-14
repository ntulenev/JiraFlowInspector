using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Carries the resolved Jira fields needed to map roadmap search results.
/// </summary>
/// <param name="RoadmapFieldId">Resolved roadmap dropdown field id.</param>
/// <param name="StartDateField">Resolved roadmap start-date field.</param>
/// <param name="EndDateField">Resolved roadmap end-date field.</param>
public sealed record RoadmapMappingContext(
    JiraFieldId RoadmapFieldId,
    RoadmapDateFieldReference StartDateField,
    RoadmapDateFieldReference EndDateField);
