using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Maps Jira issue responses into timelines.
/// </summary>
public interface IIssueTimelineMapper
{
    /// <summary>
    /// Maps one Jira issue response into a timeline.
    /// </summary>
    IssueTimeline Map(JiraIssueResponse response, IssueKey fallbackKey);
}
