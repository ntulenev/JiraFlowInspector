using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
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
    IReadOnlyList<string> BuildRequestedFields(ReleaseIssueMappingContext context);

    /// <summary>
    /// Maps search issues into release issue rows.
    /// </summary>
    IReadOnlyList<ReleaseIssueItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context);
}

