using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk changelog container per issue.
/// </summary>
public sealed class JiraBulkIssueChangelogResponse
{
    /// <summary>
    /// Gets issue id.
    /// </summary>
    [JsonPropertyName("issueId")]
    public string? IssueId { get; init; }

    /// <summary>
    /// Gets changelog entries.
    /// </summary>
    [JsonPropertyName("changeHistories")]
    public IReadOnlyList<JiraBulkHistoryResponse> ChangeHistories { get; init; } = [];
}
