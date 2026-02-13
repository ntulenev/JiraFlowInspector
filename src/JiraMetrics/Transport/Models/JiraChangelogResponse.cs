using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira changelog response DTO.
/// </summary>
internal sealed class JiraChangelogResponse
{
    /// <summary>
    /// Gets changelog histories.
    /// </summary>
    [JsonPropertyName("histories")]
    public List<JiraHistoryResponse> Histories { get; init; } = [];
}
