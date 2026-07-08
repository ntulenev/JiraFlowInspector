using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue priority DTO.
/// </summary>
public sealed class JiraPriorityResponse
{
    /// <summary>
    /// Gets priority name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
