using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api.Jql;

/// <summary>
/// Builds JQL for release issue reads.
/// </summary>
public interface IReleaseIssuesJqlBuilder
{
    /// <summary>
    /// Builds the release issue search query.
    /// </summary>
    /// <param name="request">Release issue read criteria.</param>
    /// <returns>The release issue JQL query.</returns>
    JqlQuery BuildQuery(ReleaseIssueReadRequest request);
}

