using JiraMetrics.Abstractions;
using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Builds JQL for architecture tasks queries.
/// </summary>
public sealed class ArchTasksJqlBuilder : IArchTasksJqlBuilder
{
    public ArchTasksJqlBuilder(IOptions<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _reportPeriod = (settings.Value
            ?? throw new ArgumentException("App settings value is required.", nameof(settings))).ReportPeriod;
    }

    public string BuildQuery(ArchTasksReportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.BuildJql(_reportPeriod);
    }

    private readonly JiraMetrics.Models.ValueObjects.ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591
