using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents an environment filter for release issue reads.
/// </summary>
public sealed record ReleaseEnvironmentFilter(JiraFieldName FieldName, JiraFieldValue Value);
