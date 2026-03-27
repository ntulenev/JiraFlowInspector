using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk issue fetch request DTO.
/// </summary>
public sealed class JiraBulkIssueFetchRequest
{
    /// <summary>
    /// Gets or sets requested field names.
    /// </summary>
    [JsonPropertyName("fields")]
    public IReadOnlyList<string>? Fields { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether field names should be treated as field keys.
    /// </summary>
    [JsonPropertyName("fieldsByKeys")]
    public bool FieldsByKeys { get; init; }

    /// <summary>
    /// Gets or sets requested issue ids or keys.
    /// </summary>
    [JsonPropertyName("issueIdsOrKeys")]
    public required IReadOnlyList<string> IssueIdsOrKeys { get; init; }

    /// <summary>
    /// Gets or sets requested entity properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public IReadOnlyList<string> Properties { get; init; } = [];
}
