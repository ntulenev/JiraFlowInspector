namespace JiraMetrics.Models;

/// <summary>
/// Immutable context shared by every stage of a single report run.
/// </summary>
public sealed record ReportRunContext
{
    /// <summary>
    /// Initializes a new report run context.
    /// </summary>
    /// <param name="generatedAt">Timestamp captured once for the report run.</param>
    public ReportRunContext(DateTimeOffset generatedAt)
    {
        GeneratedAt = generatedAt;
    }

    /// <summary>
    /// Gets the timestamp captured for the report run.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Creates a context using the supplied time provider.
    /// </summary>
    /// <param name="timeProvider">Source of the current time.</param>
    /// <returns>A new report run context.</returns>
    public static ReportRunContext Create(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return new ReportRunContext(timeProvider.GetLocalNow());
    }
}
