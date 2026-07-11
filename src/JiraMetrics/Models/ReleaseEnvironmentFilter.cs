using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents an environment filter for release issue reads.
/// </summary>
/// <param name="FieldName">The <paramref name="FieldName"/> value.</param>
/// <param name="Value">The <paramref name="Value"/> value.</param>
public sealed record ReleaseEnvironmentFilter(JiraFieldName FieldName, JiraFieldValue Value);
