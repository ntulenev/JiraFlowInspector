namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Raw configuration for the current Jira roadmap snapshot.
/// </summary>
public sealed class RoadmapReportOptions
{
    /// <summary>
    /// Gets or sets the JQL selecting roadmap issues.
    /// </summary>
    public string? Jql { get; init; }

    /// <summary>
    /// Gets or sets the exact Jira field name or id containing the roadmap dropdown value.
    /// </summary>
    public string? RoadmapFieldName { get; init; }

    /// <summary>
    /// Gets or sets the Jira start-date field name or id.
    /// </summary>
    public string StartDateFieldName { get; init; } = "Start date";

    /// <summary>
    /// Gets or sets the Jira end-date field name or id.
    /// </summary>
    public string EndDateFieldName { get; init; } = "End date";
}
