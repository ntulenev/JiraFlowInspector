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
    /// <param name="reporducedOnProd">Whether the issue was reproduced on production.</param>
    /// <param name="priority">Optional issue priority.</param>
    public IssueListItem(
        IssueKey key,
        IssueSummary title,
        DateTimeOffset? createdAt = null,
        bool reporducedOnProd = false,
        string? priority = null)
    {
        Key = key;
        Title = title;
        CreatedAt = createdAt;
        ReporducedOnProd = reporducedOnProd;
        Priority = string.IsNullOrWhiteSpace(priority) ? null : priority.Trim();
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

    /// <summary>
    /// Gets a value indicating whether the issue was reproduced on production.
    /// </summary>
    public bool ReporducedOnProd { get; }

    /// <summary>
    /// Gets optional issue priority.
    /// </summary>
    public string? Priority { get; }
}
