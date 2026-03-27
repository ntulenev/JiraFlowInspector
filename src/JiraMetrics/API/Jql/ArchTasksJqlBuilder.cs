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
        _monthLabel = (settings.Value
            ?? throw new ArgumentException("App settings value is required.", nameof(settings))).MonthLabel;
    }

    public string BuildQuery(ArchTasksReportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.BuildJql(_monthLabel);
    }

    private readonly JiraMetrics.Models.ValueObjects.MonthLabel _monthLabel;
}
#pragma warning restore CS1591
