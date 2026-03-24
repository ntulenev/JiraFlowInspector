using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Builds JQL for global incident reads.
/// </summary>
public interface IGlobalIncidentsJqlBuilder
{
    /// <summary>
    /// Builds the global incidents search query.
    /// </summary>
    string BuildQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields);
}
