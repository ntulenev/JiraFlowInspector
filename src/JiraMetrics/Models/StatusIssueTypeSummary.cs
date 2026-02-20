using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents issue counts grouped by status and issue type.
/// </summary>
public sealed record StatusIssueTypeSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusIssueTypeSummary"/> class.
    /// </summary>
    /// <param name="status">Issue status name.</param>
    /// <param name="count">Total issue count for status.</param>
    /// <param name="issueTypes">Issue counts grouped by type.</param>
    public StatusIssueTypeSummary(
        StatusName status,
        ItemCount count,
        IReadOnlyList<IssueTypeCountSummary> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        Status = status;
        Count = count;
        IssueTypes = [.. issueTypes];
    }

    /// <summary>
    /// Gets status name.
    /// </summary>
    public StatusName Status { get; }

    /// <summary>
    /// Gets total issue count for status.
    /// </summary>
    public ItemCount Count { get; }

    /// <summary>
    /// Gets issue counts grouped by type.
    /// </summary>
    public IReadOnlyList<IssueTypeCountSummary> IssueTypes { get; }
}
