using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira sub-task DTO.
/// </summary>
public sealed class JiraSubtaskResponse
{
    /// <summary>
    /// Gets sub-task key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}
