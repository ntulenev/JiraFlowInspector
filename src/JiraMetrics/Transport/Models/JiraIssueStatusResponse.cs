using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue status DTO.
/// </summary>
public sealed class JiraIssueStatusResponse
{
    /// <summary>
    /// Gets issue status name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
