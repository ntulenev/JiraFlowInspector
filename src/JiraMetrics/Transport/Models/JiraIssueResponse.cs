using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue response DTO.
/// </summary>
internal sealed class JiraIssueResponse
{
    /// <summary>
    /// Gets issue key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    /// <summary>
    /// Gets issue fields payload.
    /// </summary>
    [JsonPropertyName("fields")]
    public JiraIssueFieldsResponse? Fields { get; init; }

    /// <summary>
    /// Gets changelog payload.
    /// </summary>
    [JsonPropertyName("changelog")]
    public JiraChangelogResponse? Changelog { get; init; }
}
