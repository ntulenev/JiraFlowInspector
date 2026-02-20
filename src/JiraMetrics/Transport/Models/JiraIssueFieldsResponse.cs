using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue fields DTO.
/// </summary>
internal sealed class JiraIssueFieldsResponse
{
    /// <summary>
    /// Gets issue summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    /// <summary>
    /// Gets issue created timestamp.
    /// </summary>
    [JsonPropertyName("created")]
    public string? Created { get; init; }

    /// <summary>
    /// Gets issue resolution timestamp.
    /// </summary>
    [JsonPropertyName("resolutiondate")]
    public string? ResolutionDate { get; init; }

    /// <summary>
    /// Gets issue type.
    /// </summary>
    [JsonPropertyName("issuetype")]
    public JiraIssueTypeResponse? IssueType { get; init; }

    /// <summary>
    /// Gets issue status.
    /// </summary>
    [JsonPropertyName("status")]
    public JiraIssueStatusResponse? Status { get; init; }

    /// <summary>
    /// Gets issue links.
    /// </summary>
    [JsonPropertyName("issuelinks")]
    public IReadOnlyList<JiraIssueLinkResponse> IssueLinks { get; init; } = [];

    /// <summary>
    /// Gets issue sub-tasks.
    /// </summary>
    [JsonPropertyName("subtasks")]
    public IReadOnlyList<JiraSubtaskResponse> Subtasks { get; init; } = [];

    /// <summary>
    /// Gets additional fields payload (for custom fields).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
}
