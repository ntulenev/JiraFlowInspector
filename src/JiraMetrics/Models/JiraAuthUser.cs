using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents authenticated Jira user information.
/// </summary>
public sealed record JiraAuthUser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraAuthUser"/> class.
    /// </summary>
    /// <param name="displayName">User display name.</param>
    /// <param name="emailAddress">Optional user email.</param>
    /// <param name="accountId">Optional account identifier.</param>
    public JiraAuthUser(
        UserDisplayName displayName,
        string? emailAddress,
        string? accountId)
    {
        DisplayName = displayName;
        EmailAddress = emailAddress;
        AccountId = accountId;
    }

    /// <summary>
    /// Gets the user display name.
    /// </summary>
    public UserDisplayName DisplayName { get; }

    /// <summary>
    /// Gets the optional email address.
    /// </summary>
    public string? EmailAddress { get; }

    /// <summary>
    /// Gets the optional account id.
    /// </summary>
    public string? AccountId { get; }
}
