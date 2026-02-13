using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents aggregated report counters for path group output.
/// </summary>
public sealed record PathGroupsSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathGroupsSummary"/> class.
    /// </summary>
    /// <param name="successfulCount">Successfully loaded issues count.</param>
    /// <param name="matchedStageCount">Issues matching stage count.</param>
    /// <param name="failedCount">Failed issues count.</param>
    /// <param name="pathGroupCount">Path groups count.</param>
    public PathGroupsSummary(
        ItemCount successfulCount,
        ItemCount matchedStageCount,
        ItemCount failedCount,
        ItemCount pathGroupCount)
    {
        SuccessfulCount = successfulCount;
        MatchedStageCount = matchedStageCount;
        FailedCount = failedCount;
        PathGroupCount = pathGroupCount;
    }

    /// <summary>
    /// Gets successful issues count.
    /// </summary>
    public ItemCount SuccessfulCount { get; }

    /// <summary>
    /// Gets matched stage issues count.
    /// </summary>
    public ItemCount MatchedStageCount { get; }

    /// <summary>
    /// Gets failed issues count.
    /// </summary>
    public ItemCount FailedCount { get; }

    /// <summary>
    /// Gets path groups count.
    /// </summary>
    public ItemCount PathGroupCount { get; }
}
