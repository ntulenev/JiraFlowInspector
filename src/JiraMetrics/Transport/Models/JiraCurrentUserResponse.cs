using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira current user API response.
/// </summary>
public sealed class JiraCurrentUserResponse
{
    /// <summary>
    /// Gets user display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets user email address.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; init; }

    /// <summary>
    /// Gets user account id.
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; init; }
}
