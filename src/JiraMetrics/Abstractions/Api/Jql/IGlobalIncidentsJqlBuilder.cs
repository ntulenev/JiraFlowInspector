using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Api.Jql;

/// <summary>
/// Builds JQL for global incident reads.
/// </summary>
public interface IGlobalIncidentsJqlBuilder
{
    /// <summary>
    /// Builds the global incidents search query.
    /// </summary>
    JqlQuery BuildQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields);
}

