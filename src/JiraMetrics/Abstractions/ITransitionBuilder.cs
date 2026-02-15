using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Builds ordered transition events from raw transition tuples.
/// </summary>
public interface ITransitionBuilder
{
    /// <summary>
    /// Builds transition events from raw transitions.
    /// </summary>
    /// <param name="rawTransitions">Raw transition tuples.</param>
    /// <param name="created">Issue creation timestamp.</param>
    /// <returns>Ordered transition events.</returns>
    IReadOnlyList<TransitionEvent> BuildTransitions(
        IReadOnlyList<(DateTimeOffset At, StatusName From, StatusName To)> rawTransitions,
        DateTimeOffset created);
}
