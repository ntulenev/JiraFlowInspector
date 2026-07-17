namespace JiraMetrics.Models;

/// <summary>
/// Contains ratio snapshots and automated test coverage used by report sections.
/// </summary>
public sealed class JiraReportRatioData
{
    /// <summary>
    /// Gets the optional all-tasks ratio snapshot.
    /// </summary>
    public IssueRatioSnapshot? AllTasks { get; init; }

    /// <summary>
    /// Gets the optional bug ratio snapshot.
    /// </summary>
    public IssueRatioSnapshot? Bugs { get; init; }

    /// <summary>
    /// Gets the optional internal-incident ratio snapshot.
    /// </summary>
    public IssueRatioSnapshot? InternalIncidents { get; init; }

    /// <summary>
    /// Gets automated test coverage for the selected period.
    /// </summary>
    public TestCoverageSnapshot TestCoverage { get; init; } = TestCoverageSnapshot.Empty;
}
