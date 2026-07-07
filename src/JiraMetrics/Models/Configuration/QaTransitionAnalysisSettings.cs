namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated settings for QA transition analysis.
/// </summary>
public sealed record QaTransitionAnalysisSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QaTransitionAnalysisSettings"/> class.
    /// </summary>
    public QaTransitionAnalysisSettings(
        bool enabled,
        IReadOnlyList<TransitionMeasurementRule> pickupTransitions,
        IReadOnlyList<TransitionMeasurementRule> testingTransitions)
    {
        ArgumentNullException.ThrowIfNull(pickupTransitions);
        ArgumentNullException.ThrowIfNull(testingTransitions);

        Enabled = enabled;
        PickupTransitions = pickupTransitions;
        TestingTransitions = testingTransitions;
    }

    /// <summary>
    /// Gets default QA transition analysis settings.
    /// </summary>
    public static QaTransitionAnalysisSettings Default { get; } = new(
        enabled: true,
        pickupTransitions:
        [
            new TransitionMeasurementRule(
                new("Quality Assurance"),
                new("QA IN PROGRESS"))
        ],
        testingTransitions:
        [
            new TransitionMeasurementRule(
                new("QA in progress"),
                new("Release Candidate")),
            new TransitionMeasurementRule(
                new("QA"),
                new("Release Candidate"))
        ]);

    /// <summary>
    /// Gets whether QA transition analysis should be rendered.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets transition rules used to measure how quickly QA takes issues in work.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementRule> PickupTransitions { get; }

    /// <summary>
    /// Gets transition rules used to measure testing time.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementRule> TestingTransitions { get; }
}
