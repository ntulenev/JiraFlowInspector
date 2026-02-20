using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira field descriptor DTO.
/// </summary>
internal sealed class JiraFieldResponse
{
    /// <summary>
    /// Gets field id.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// Gets field display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
