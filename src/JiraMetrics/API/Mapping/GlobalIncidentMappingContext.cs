using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Carries the resolved Jira fields needed to map incident search results.
/// </summary>
/// <param name="IncidentStartFields">Primary and optional fallback start-date fields.</param>
/// <param name="IncidentRecoveryFields">Primary and optional fallback recovery-date fields.</param>
/// <param name="ImpactFieldId">Resolved impact field id when available.</param>
/// <param name="ImpactFieldName">Configured impact field name.</param>
/// <param name="UrgencyFieldId">Resolved urgency field id when available.</param>
/// <param name="UrgencyFieldName">Configured urgency field name.</param>
/// <param name="AdditionalFieldIds">Additional resolved fields keyed by configured field name.</param>
public sealed record GlobalIncidentMappingContext(
    IReadOnlyList<ResolvedJiraField> IncidentStartFields,
    IReadOnlyList<ResolvedJiraField> IncidentRecoveryFields,
    JiraFieldId? ImpactFieldId,
    JiraFieldName ImpactFieldName,
    JiraFieldId? UrgencyFieldId,
    JiraFieldName UrgencyFieldName,
    IReadOnlyDictionary<JiraFieldName, JiraFieldId?> AdditionalFieldIds);
