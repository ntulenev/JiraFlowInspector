using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api.Mapping;

/// <summary>
/// Maps Jira search responses into roadmap rows.
/// </summary>
public interface IRoadmapItemMapper
{
    /// <summary>
    /// Builds the field list required for roadmap mapping.
    /// </summary>
    JiraSearchFields BuildRequestedFields(RoadmapMappingContext context);

    /// <summary>
    /// Maps search issues into roadmap rows.
    /// </summary>
    IReadOnlyList<RoadmapItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        RoadmapMappingContext context);
}
