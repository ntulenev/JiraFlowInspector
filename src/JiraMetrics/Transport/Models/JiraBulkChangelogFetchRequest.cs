using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk changelog fetch request DTO.
/// </summary>
public sealed class JiraBulkChangelogFetchRequest
{
    /// <summary>
    /// Gets or sets filtered field ids.
    /// </summary>
    [JsonPropertyName("fieldIds")]
    public IReadOnlyList<string>? FieldIds { get; init; }

    /// <summary>
    /// Gets or sets requested issue ids or keys.
    /// </summary>
    [JsonPropertyName("issueIdsOrKeys")]
    public required IReadOnlyList<string> IssueIdsOrKeys { get; init; }

    /// <summary>
    /// Gets or sets changelog page size.
    /// </summary>
    [JsonPropertyName("maxResults")]
    public int MaxResults { get; init; }

    /// <summary>
    /// Gets or sets pagination token.
    /// </summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}
