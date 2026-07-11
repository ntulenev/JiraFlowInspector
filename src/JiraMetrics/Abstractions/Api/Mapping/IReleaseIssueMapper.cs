using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api.Mapping;

/// <summary>
/// Maps Jira search responses into release issue rows.
/// </summary>
public interface IReleaseIssueMapper
{
    /// <summary>
    /// Builds the field list required for release mapping.
    /// </summary>
    /// <param name="context">The <paramref name="context"/> value.</param>
    JiraSearchFields BuildRequestedFields(ReleaseIssueMappingContext context);

    /// <summary>
    /// Maps search issues into release issue rows.
    /// </summary>
    /// <param name="issues">The <paramref name="issues"/> value.</param>
    /// <param name="context">The <paramref name="context"/> value.</param>
    IReadOnlyList<ReleaseIssueItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context);
}

