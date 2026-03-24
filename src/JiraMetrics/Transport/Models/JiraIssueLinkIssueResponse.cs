using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira linked issue DTO.
/// </summary>
public sealed class JiraIssueLinkIssueResponse
{
    /// <summary>
    /// Gets linked issue key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}
