using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api;

internal interface IJiraIssueTimelineClient
{
    Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken);

    Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        CancellationToken cancellationToken);
}

