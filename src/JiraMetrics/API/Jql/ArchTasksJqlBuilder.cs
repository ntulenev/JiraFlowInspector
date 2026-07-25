using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Builds JQL for architecture tasks queries.
/// </summary>
public sealed class ArchTasksJqlBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchTasksJqlBuilder"/> class.
    /// </summary>
    /// <param name="settings">Application settings containing the report period.</param>
    public ArchTasksJqlBuilder(IOptions<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _reportPeriod = settings.Value.ReportPeriod;
    }

    /// <summary>
    /// Builds the architecture tasks query for the configured report period.
    /// </summary>
    /// <param name="settings">Architecture tasks report settings.</param>
    /// <returns>The JQL query.</returns>
    public JqlQuery BuildQuery(ArchTasksReportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new JqlQuery(settings.BuildJql(_reportPeriod));
    }

    private readonly JiraMetrics.Models.ValueObjects.ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591

