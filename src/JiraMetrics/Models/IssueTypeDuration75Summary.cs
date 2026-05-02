using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents P75 duration summary for a specific issue type.
/// </summary>
public sealed record IssueTypeDuration75Summary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTypeDuration75Summary"/> class.
    /// </summary>
    /// <param name="issueType">Issue type.</param>
    /// <param name="issueCount">Number of issues used in percentile sample.</param>
    /// <param name="durationP75">P75 duration.</param>
    public IssueTypeDuration75Summary(
        IssueTypeName issueType,
        ItemCount issueCount,
        TimeSpan durationP75)
    {
        IssueType = issueType;
        IssueCount = issueCount;
        DurationP75 = durationP75 < TimeSpan.Zero ? TimeSpan.Zero : durationP75;
    }

    /// <summary>
    /// Gets issue type.
    /// </summary>
    public IssueTypeName IssueType { get; }

    /// <summary>
    /// Gets number of issues used in percentile sample.
    /// </summary>
    public ItemCount IssueCount { get; }

    /// <summary>
    /// Gets P75 duration.
    /// </summary>
    public TimeSpan DurationP75 { get; }
}
