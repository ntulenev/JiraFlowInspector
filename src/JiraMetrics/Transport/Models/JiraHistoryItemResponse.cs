using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira changelog history item DTO.
/// </summary>
public sealed class JiraHistoryItemResponse
{
    /// <summary>
    /// Gets changed field name.
    /// </summary>
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    /// <summary>
    /// Gets previous status name.
    /// </summary>
    [JsonPropertyName("fromString")]
    public string? FromStatus { get; init; }

    /// <summary>
    /// Gets new status name.
    /// </summary>
    [JsonPropertyName("toString")]
    public string? ToStatus { get; init; }
}
