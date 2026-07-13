namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated settings for the current Jira roadmap snapshot.
/// </summary>
public sealed record RoadmapReportSettings
{
    /// <summary>
    /// Initializes roadmap report settings.
    /// </summary>
    public RoadmapReportSettings(
        string jql,
        string roadmapFieldName,
        string startDateFieldName = "Start date",
        string endDateFieldName = "End date")
    {
        Jql = NormalizeRequired(jql, nameof(jql));
        RoadmapFieldName = NormalizeRequired(roadmapFieldName, nameof(roadmapFieldName));
        StartDateFieldName = NormalizeRequired(startDateFieldName, nameof(startDateFieldName));
        EndDateFieldName = NormalizeRequired(endDateFieldName, nameof(endDateFieldName));
    }

    /// <summary>Gets the JQL selecting roadmap issues.</summary>
    public string Jql { get; }

    /// <summary>Gets the exact Jira roadmap dropdown field name or id.</summary>
    public string RoadmapFieldName { get; }

    /// <summary>Gets the Jira start-date field name or id.</summary>
    public string StartDateFieldName { get; }

    /// <summary>Gets the Jira end-date field name or id.</summary>
    public string EndDateFieldName { get; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
