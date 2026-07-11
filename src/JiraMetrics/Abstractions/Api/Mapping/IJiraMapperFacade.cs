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
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    IReadOnlyList<IssueKey> MapIssueKeys(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Maps search issues into lightweight issue list rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="context">The <paramref name="context"/> value.</param>
    IReadOnlyList<IssueListItem> MapIssueListItems(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        IssueListMappingContext? context = null);

    /// <summary>
    /// Maps search issues into grouped status and issue-type summaries.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    IReadOnlyList<StatusIssueTypeSummary> MapStatusIssueTypeSummaries(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Maps search issues into architecture task rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    IReadOnlyList<ArchTaskItem> MapArchTaskItems(IReadOnlyList<JiraIssueKeyResponse> issues);

    /// <summary>
    /// Builds the field list required for release mapping.
    /// </summary>
    /// <param name="context">The <paramref name="context"/> value.</param>
    JiraSearchFields BuildReleaseRequestedFields(ReleaseIssueMappingContext context);

    /// <summary>
    /// Maps search issues into release issue rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="context">The <paramref name="context"/> value.</param>
    IReadOnlyList<ReleaseIssueItem> MapReleaseIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context);

    /// <summary>
    /// Builds the field list required for global incident mapping.
    /// </summary>
    /// <param name="context">The <paramref name="context"/> value.</param>
    JiraSearchFields BuildGlobalIncidentRequestedFields(GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps search issues into global incident rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="context">The <paramref name="context"/> value.</param>
    IReadOnlyList<GlobalIncidentItem> MapGlobalIncidents(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context);

    /// <summary>
    /// Maps one Jira issue response into a timeline.
    /// </summary>
    /// <param name="response">The <paramref name="response"/> value.</param>
    /// <param name="fallbackKey">The <paramref name="fallbackKey"/> value.</param>
    IssueTimeline MapIssueTimeline(JiraIssueResponse response, IssueKey fallbackKey);
}

