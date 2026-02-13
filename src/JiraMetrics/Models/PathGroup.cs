using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents issues grouped by the same transition path.
/// </summary>
public sealed record PathGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathGroup"/> class.
    /// </summary>
    /// <param name="pathLabel">Path label for display.</param>
    /// <param name="issues">Issues in the group.</param>
    /// <param name="p75Transitions">P75 durations for each transition in the path.</param>
    /// <param name="totalP75">Total P75 duration for the path.</param>
    public PathGroup(
        PathLabel pathLabel,
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<PercentileTransition> p75Transitions,
        TimeSpan totalP75)
    {
        PathLabel = pathLabel;
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
        P75Transitions = p75Transitions ?? throw new ArgumentNullException(nameof(p75Transitions));
        TotalP75 = totalP75;
    }

    /// <summary>
    /// Gets path label.
    /// </summary>
    public PathLabel PathLabel { get; }

    /// <summary>
    /// Gets issues in this group.
    /// </summary>
    public IReadOnlyList<IssueTimeline> Issues { get; }

    /// <summary>
    /// Gets P75 transitions for this path.
    /// </summary>
    public IReadOnlyList<PercentileTransition> P75Transitions { get; }

    /// <summary>
    /// Gets total P75 duration.
    /// </summary>
    public TimeSpan TotalP75 { get; }
}
