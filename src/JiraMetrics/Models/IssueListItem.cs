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
    public IssueListItem(IssueKey key, IssueSummary title)
    {
        Key = key;
        Title = title;
    }

    /// <summary>
    /// Gets issue key.
    /// </summary>
    public IssueKey Key { get; }

    /// <summary>
    /// Gets issue title.
    /// </summary>
    public IssueSummary Title { get; }
}
