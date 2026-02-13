using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue search response.
/// </summary>
internal sealed class JiraSearchResponse
{
    /// <summary>
    /// Gets returned issues.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<JiraIssueKeyResponse> Issues { get; init; } = [];

    /// <summary>
    /// Gets whether this is the last page.
    /// </summary>
    [JsonPropertyName("isLast")]
    public bool IsLast { get; init; }

    /// <summary>
    /// Gets next page token.
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}
