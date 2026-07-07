namespace JiraMetrics.Models.Configuration;

/// <summary>
/// QA transition analysis options.
/// </summary>
public sealed class QaTransitionAnalysisOptions
{
    /// <summary>
    /// Gets or sets whether QA transition analysis should be rendered.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets transition rules used to measure how quickly QA takes issues in work.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementRuleOptions>? PickupTransitions { get; init; }

    /// <summary>
    /// Gets or sets transition rules used to measure testing time.
    /// </summary>
    public IReadOnlyList<TransitionMeasurementRuleOptions>? TestingTransitions { get; init; }
}
