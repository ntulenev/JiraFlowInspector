using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira changelog response DTO.
/// </summary>
public sealed class JiraChangelogResponse
{
    /// <summary>
    /// Gets changelog histories.
    /// </summary>
    [JsonPropertyName("histories")]
    public IReadOnlyList<JiraHistoryResponse> Histories { get; init; } = [];
}
