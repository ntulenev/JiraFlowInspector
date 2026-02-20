using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a lightweight issue list row.
/// </summary>
public sealed record IssueListItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueListItem"/> class.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="title">Issue title.</param>
    /// <param name="createdAt">Optional issue creation timestamp.</param>
    public IssueListItem(IssueKey key, IssueSummary title, DateTimeOffset? createdAt = null)
    {
        Key = key;
        Title = title;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets issue key.
    /// </summary>
    public IssueKey Key { get; }

    /// <summary>
    /// Gets issue title.
    /// </summary>
    public IssueSummary Title { get; }

    /// <summary>
    /// Gets optional issue creation timestamp.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; }
}
