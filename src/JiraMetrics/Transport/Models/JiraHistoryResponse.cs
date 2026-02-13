using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira changelog history DTO.
/// </summary>
internal sealed class JiraHistoryResponse
{
    /// <summary>
    /// Gets history creation timestamp.
    /// </summary>
    [JsonPropertyName("created")]
    public string? Created { get; init; }

    /// <summary>
    /// Gets history items.
    /// </summary>
    [JsonPropertyName("items")]
    public List<JiraHistoryItemResponse> Items { get; init; } = [];
}
