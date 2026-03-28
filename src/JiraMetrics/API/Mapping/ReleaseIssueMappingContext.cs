using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Carries the resolved Jira fields needed to map release search results.
/// </summary>
/// <param name="ReleaseFieldId">Resolved release date field id.</param>
/// <param name="ReleaseDateFieldName">Configured release date field name.</param>
/// <param name="ComponentsFieldId">Resolved components field id when available.</param>
/// <param name="ComponentsFieldName">Configured components field name when available.</param>
/// <param name="HotFixRules">Resolved hot-fix marker rules.</param>
/// <param name="RollbackFieldId">Resolved rollback field id when available.</param>
/// <param name="RollbackFieldName">Configured rollback field name.</param>
/// <param name="EnvironmentFieldId">Resolved environment field id when available.</param>
/// <param name="EnvironmentFieldName">Configured environment field name when available.</param>
public sealed record ReleaseIssueMappingContext(
    JiraFieldId ReleaseFieldId,
    JiraFieldName ReleaseDateFieldName,
    JiraFieldId? ComponentsFieldId,
    JiraFieldName? ComponentsFieldName,
    IReadOnlyList<ResolvedHotFixRule> HotFixRules,
    JiraFieldId? RollbackFieldId,
    JiraFieldName RollbackFieldName,
    JiraFieldId? EnvironmentFieldId,
    JiraFieldName? EnvironmentFieldName);
