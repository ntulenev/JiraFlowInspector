using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Abstractions.Api.Jql;

/// <summary>
/// Builds JQL queries for architecture tasks reports.
/// </summary>
public interface IArchTasksJqlBuilder
{
    /// <summary>
    /// Builds the final search query for architecture tasks.
    /// </summary>
    /// <param name="settings">Architecture tasks settings.</param>
    /// <returns>Final JQL query.</returns>
    string BuildQuery(ArchTasksReportSettings settings);
}

