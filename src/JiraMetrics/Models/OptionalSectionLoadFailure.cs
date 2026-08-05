using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Describes an optional report section that could not be loaded.
/// </summary>
/// <param name="Section">Failed optional section.</param>
/// <param name="Error">Load failure details.</param>
public sealed record OptionalSectionLoadFailure(
    OptionalReportSection Section,
    ErrorMessage Error);
