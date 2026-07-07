using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents QA-specific transition measurements.
/// </summary>
public sealed record QaTransitionAnalysis
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QaTransitionAnalysis"/> class.
    /// </summary>
    public QaTransitionAnalysis(
        ItemCount analyzedIssueCount,
        IReadOnlyList<TransitionMeasurementIssue> pickupIssues,
        TimeSpan? pickupDuration75,
        IReadOnlyList<IssueTypeDuration75Summary> pickupDuration75PerType,
        IReadOnlyList<TransitionMeasurementIssue> testingIssues,
        TimeSpan? testingDuration75,
        IReadOnlyList<IssueTypeDuration75Summary> testingDuration75PerType)
    {
        ArgumentNullException.ThrowIfNull(pickupIssues);
        ArgumentNullException.ThrowIfNull(pickupDuration75PerType);
        ArgumentNullException.ThrowIfNull(testingIssues);
        ArgumentNullException.ThrowIfNull(testingDuration75PerType);

        AnalyzedIssueCount = analyzedIssueCount;
        PickupIssues = pickupIssues;
        PickupDuration75 = pickupDuration75;
        PickupDuration75PerType = pickupDuration75PerType;
        TestingIssues = testingIssues;
        TestingDuration75 = testingDuration75;
        TestingDuration75PerType = testingDuration75PerType;
    }

    /// <summary>
    /// Gets empty QA analysis.
    /// </summary>
    public static QaTransitionAnalysis Empty { get; } = new(
        new ItemCount(0),
        [],
        null,
        [],
        [],
        null,
        []);

    /// <summary>
    /// Gets distinct issue count used as denominator for QA percentages.
    /// </summary>
    public ItemCount AnalyzedIssueCount { get; }

    /// <summary>
    /// Gets issues matched by configured QA pickup transition rules.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementIssue> PickupIssues { get; }

    /// <summary>
    /// Gets overall P75 duration for QA pickup.
    /// </summary>
    public TimeSpan? PickupDuration75 { get; }

    /// <summary>
    /// Gets QA pickup P75 grouped by issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeDuration75Summary> PickupDuration75PerType { get; }

    /// <summary>
    /// Gets testing time measurements.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementIssue> TestingIssues { get; }

    /// <summary>
    /// Gets overall P75 duration for testing time.
    /// </summary>
    public TimeSpan? TestingDuration75 { get; }

    /// <summary>
    /// Gets testing time P75 grouped by issue type.
    /// </summary>
    public IReadOnlyList<IssueTypeDuration75Summary> TestingDuration75PerType { get; }

    /// <summary>
    /// Gets percentage of analyzed issues with QA pickup transition.
    /// </summary>
    public decimal PickupIssuePercentage => AnalyzedIssueCount.Value == 0
        ? 0m
        : decimal.Divide(PickupIssues.Count * 100, AnalyzedIssueCount.Value);
}
