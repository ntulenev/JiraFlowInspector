using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Builds JQL for release issue reads.
/// </summary>
public interface IReleaseIssuesJqlBuilder
{
    /// <summary>
    /// Builds the release issue search query.
    /// </summary>
    string BuildQuery(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? environmentFieldName,
        string? environmentFieldValue);
}
