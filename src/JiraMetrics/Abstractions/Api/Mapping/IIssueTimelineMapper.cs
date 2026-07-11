using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions.Api.Mapping;

/// <summary>
/// Maps Jira issue responses into timelines.
/// </summary>
public interface IIssueTimelineMapper
{
    /// <summary>
    /// Maps one Jira issue response into a timeline.
    /// </summary>
    /// <param name="response">The <paramref name="response"/> value.</param>
    /// <param name="fallbackKey">The <paramref name="fallbackKey"/> value.</param>
    /// <returns>The result of the operation.</returns>
    IssueTimeline Map(JiraIssueResponse response, IssueKey fallbackKey);
}

