using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue-link type DTO.
/// </summary>
public sealed class JiraIssueLinkTypeResponse
{
    /// <summary>
    /// Gets inward relation text.
    /// </summary>
    [JsonPropertyName("inward")]
    public string? Inward { get; init; }

    /// <summary>
    /// Gets outward relation text.
    /// </summary>
    [JsonPropertyName("outward")]
    public string? Outward { get; init; }
}
