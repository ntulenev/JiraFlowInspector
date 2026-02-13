using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue type DTO.
/// </summary>
internal sealed class JiraIssueTypeResponse
{
    /// <summary>
    /// Gets issue type name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
