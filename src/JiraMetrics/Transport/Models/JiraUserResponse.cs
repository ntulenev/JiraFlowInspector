using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira user DTO embedded in issue fields.
/// </summary>
public sealed class JiraUserResponse
{
    /// <summary>
    /// Gets user display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}
