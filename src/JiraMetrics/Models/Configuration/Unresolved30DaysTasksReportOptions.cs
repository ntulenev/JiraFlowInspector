namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Unresolved tasks older than 30 days report options.
/// </summary>
public sealed class Unresolved30DaysTasksReportOptions
{
    /// <summary>
    /// Gets or sets raw JQL used to load unresolved tasks older than 30 days.
    /// </summary>
    public string? Jql { get; init; }
}
