using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents issue count grouped by issue type.
/// </summary>
public sealed record IssueTypeCountSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTypeCountSummary"/> class.
    /// </summary>
    /// <param name="issueType">Issue type name.</param>
    /// <param name="count">Issue count for type.</param>
    public IssueTypeCountSummary(IssueTypeName issueType, ItemCount count)
    {
        IssueType = issueType;
        Count = count;
    }

    /// <summary>
    /// Gets issue type name.
    /// </summary>
    public IssueTypeName IssueType { get; }

    /// <summary>
    /// Gets issue count for type.
    /// </summary>
    public ItemCount Count { get; }
}
