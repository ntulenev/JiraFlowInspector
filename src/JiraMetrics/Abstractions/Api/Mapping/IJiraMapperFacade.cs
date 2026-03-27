using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api.Mapping;

/// <summary>
/// Facade for mapping Jira transport models into domain models.
/// </summary>
public interface IJiraMapperFacade
{
    /// <summary>
    /// Maps search issues into distinct ordered issue keys.
    /// </summary>
    IReadOnlyList<IssueKey> MapIssueKeys(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Maps search issues into lightweight issue list rows.
    /// </summary>
    IReadOnlyList<IssueListItem> MapIssueListItems(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Maps search issues into grouped status and issue-type summaries.
    /// </summary>
    IReadOnlyList<StatusIssueTypeSummary> MapStatusIssueTypeSummaries(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Maps search issues into architecture task rows.
    /// </summary>
    IReadOnlyList<ArchTaskItem> MapArchTaskItems(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Builds the field list required for release mapping.
    /// </summary>
    JiraSearchFields BuildReleaseRequestedFields(ReleaseIssueMappingContext context);

    /// <summary>
    /// Maps search issues into release issue rows.
    /// </summary>
    IReadOnlyList<ReleaseIssueItem> MapReleaseIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context);

    /// <summary>
    /// Builds the field list required for global incident mapping.
    /// </summary>
    JiraSearchFields BuildGlobalIncidentRequestedFields(GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps search issues into global incident rows.
    /// </summary>
    IReadOnlyList<GlobalIncidentItem> MapGlobalIncidents(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps one Jira issue response into a timeline.
    /// </summary>
    IssueTimeline MapIssueTimeline(JiraIssueResponse response, IssueKey fallbackKey);
}

