using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Context for mapping lightweight issue rows.
/// </summary>
/// <param name="ReporducedOnProdFieldId">Optional Jira field id for production reproduction marker.</param>
/// <param name="ReporducedOnProdFieldName">Optional Jira field name for production reproduction marker.</param>
public sealed record IssueListMappingContext(
    JiraFieldId? ReporducedOnProdFieldId,
    JiraFieldName? ReporducedOnProdFieldName);
