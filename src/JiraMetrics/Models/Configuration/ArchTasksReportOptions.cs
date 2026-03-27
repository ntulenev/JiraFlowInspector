namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Architecture tasks report options.
/// </summary>
public sealed class ArchTasksReportOptions
{
    /// <summary>
    /// Gets or sets raw JQL or JQL template used for architecture tasks search.
    /// </summary>
    public string? Jql { get; init; }
}
