using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk issue fetch response DTO.
/// </summary>
public sealed class JiraBulkIssueFetchResponse
{
    /// <summary>
    /// Gets returned issues.
    /// </summary>
    [JsonPropertyName("issues")]
    public IReadOnlyList<JiraIssueResponse> Issues { get; init; } = [];
}
