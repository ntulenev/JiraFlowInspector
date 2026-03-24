using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue-link DTO.
/// </summary>
public sealed class JiraIssueLinkResponse
{
    /// <summary>
    /// Gets issue-link type.
    /// </summary>
    [JsonPropertyName("type")]
    public JiraIssueLinkTypeResponse? Type { get; init; }

    /// <summary>
    /// Gets inward linked issue.
    /// </summary>
    [JsonPropertyName("inwardIssue")]
    public JiraIssueLinkIssueResponse? InwardIssue { get; init; }

    /// <summary>
    /// Gets outward linked issue.
    /// </summary>
    [JsonPropertyName("outwardIssue")]
    public JiraIssueLinkIssueResponse? OutwardIssue { get; init; }
}
