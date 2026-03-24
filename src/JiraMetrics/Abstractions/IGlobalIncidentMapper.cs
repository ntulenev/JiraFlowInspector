using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Maps Jira search responses into global incident rows.
/// </summary>
public interface IGlobalIncidentMapper
{
    /// <summary>
    /// Builds the field list required for global incident mapping.
    /// </summary>
    IReadOnlyList<string> BuildRequestedFields(GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps search issues into global incident rows.
    /// </summary>
    IReadOnlyList<GlobalIncidentItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context);
}
