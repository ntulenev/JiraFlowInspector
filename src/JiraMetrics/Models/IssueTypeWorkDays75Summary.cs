using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents days-at-work P75 summary for a specific issue type.
/// </summary>
public sealed record IssueTypeWorkDays75Summary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTypeWorkDays75Summary"/> class.
    /// </summary>
    /// <param name="issueType">Issue type.</param>
    /// <param name="issueCount">Number of issues used in percentile sample.</param>
    /// <param name="daysAtWorkP75">P75 working duration for issue type.</param>
    public IssueTypeWorkDays75Summary(
        IssueTypeName issueType,
        ItemCount issueCount,
        TimeSpan daysAtWorkP75)
    {
        IssueType = issueType;
        IssueCount = issueCount;
        DaysAtWorkP75 = daysAtWorkP75 < TimeSpan.Zero ? TimeSpan.Zero : daysAtWorkP75;
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
    /// Gets P75 working duration for issue type.
    /// </summary>
    public TimeSpan DaysAtWorkP75 { get; }
}
