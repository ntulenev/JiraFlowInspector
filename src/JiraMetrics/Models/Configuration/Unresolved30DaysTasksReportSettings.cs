namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated unresolved tasks older than 30 days report settings.
/// </summary>
public sealed record Unresolved30DaysTasksReportSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Unresolved30DaysTasksReportSettings"/> class.
    /// </summary>
    /// <param name="jql">Raw JQL used to load unresolved tasks older than 30 days.</param>
    public Unresolved30DaysTasksReportSettings(string jql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jql);
        Jql = jql.Trim();
    }

    /// <summary>
    /// Gets raw JQL used to load unresolved tasks older than 30 days.
    /// </summary>
    public string Jql { get; }
}
