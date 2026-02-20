using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue key DTO.
/// </summary>
internal sealed class JiraIssueKeyResponse
{
    /// <summary>
    /// Gets issue key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    /// <summary>
    /// Gets issue fields included in search response.
    /// </summary>
    [JsonPropertyName("fields")]
    public JiraIssueFieldsResponse? Fields { get; init; }
}
