using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api.Mapping;

/// <summary>
/// Maps Jira search responses into global incident rows.
/// </summary>
public interface IGlobalIncidentMapper
{
    /// <summary>
    /// Builds the field list required for global incident mapping.
    /// </summary>
    /// <param name="context">The <paramref name="context"/> value.</param>
    /// <returns>The result of the operation.</returns>
    JiraSearchFields BuildRequestedFields(GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps search issues into global incident rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="context">The <paramref name="context"/> value.</param>
    /// <returns>The result of the operation.</returns>
    IReadOnlyList<GlobalIncidentItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context);
}

