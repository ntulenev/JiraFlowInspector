using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Automated test coverage snapshot for completed issues.
/// </summary>
/// <param name="DoneIssues">The <paramref name="DoneIssues"/> value.</param>
/// <param name="CoveredIssues">The <paramref name="CoveredIssues"/> value.</param>
public sealed record TestCoverageSnapshot(
    IReadOnlyList<IssueListItem> DoneIssues,
    IReadOnlyList<IssueListItem> CoveredIssues)
{
    /// <summary>
    /// Gets an empty automated test coverage snapshot.
    /// </summary>
    public static TestCoverageSnapshot Empty { get; } = new([], []);

    /// <summary>
    /// Gets total number of completed issues in the configured scope.
    /// </summary>
    public ItemCount TotalIssues { get; } = new(DoneIssues.Count);

    /// <summary>
    /// Gets number of completed issues covered by automated tests.
    /// </summary>
    public ItemCount CoveredIssueCount { get; } = new(CoveredIssues.Count);

    /// <summary>
    /// Gets automated test coverage percent.
    /// </summary>
    public double? CoveragePercentage => DoneIssues.Count == 0
        ? null
        : CoveredIssues.Count * 100.0 / DoneIssues.Count;
}
