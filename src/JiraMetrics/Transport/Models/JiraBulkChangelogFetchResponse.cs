using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk changelog fetch response DTO.
/// </summary>
public sealed class JiraBulkChangelogFetchResponse
{
    /// <summary>
    /// Gets grouped changelogs per issue id.
    /// </summary>
    [JsonPropertyName("issueChangeLogs")]
    public IReadOnlyList<JiraBulkIssueChangelogResponse> IssueChangeLogs { get; init; } = [];

    /// <summary>
    /// Gets next page token.
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}
