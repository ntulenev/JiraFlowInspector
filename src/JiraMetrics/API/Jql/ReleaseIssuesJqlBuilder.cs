using JiraMetrics.Abstractions;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Helpers;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Builds JQL for release issue queries.
/// </summary>
public sealed class ReleaseIssuesJqlBuilder : IReleaseIssuesJqlBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseIssuesJqlBuilder"/> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public ReleaseIssuesJqlBuilder(IOptions<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _monthLabel = (settings.Value
            ?? throw new ArgumentException("App settings value is required.", nameof(settings))).MonthLabel;
    }

    public string BuildQuery(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? environmentFieldName,
        string? environmentFieldValue)
    {
        var (monthStart, nextMonthStart) = _monthLabel.GetMonthRange();
        var escapedProject = releaseProjectKey.Value.EscapeJqlString();
        var escapedLabel = projectLabel.EscapeJqlString();
        var escapedFieldName = releaseDateFieldName.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\"",
            $"labels = \"{escapedLabel}\"",
            $"\"{escapedFieldName}\" >= \"{monthStart:yyyy-MM-dd}\"",
            $"\"{escapedFieldName}\" < \"{nextMonthStart:yyyy-MM-dd}\""
        };

        if (!string.IsNullOrWhiteSpace(environmentFieldName)
            && !string.IsNullOrWhiteSpace(environmentFieldValue))
        {
            var escapedEnvironmentFieldName = environmentFieldName.EscapeJqlString();
            var escapedEnvironmentFieldValue = environmentFieldValue.EscapeJqlString();
            clauses.Add($"\"{escapedEnvironmentFieldName}\" = \"{escapedEnvironmentFieldValue}\"");
        }

        return $"{string.Join(" AND ", clauses)} ORDER BY \"{escapedFieldName}\" ASC, key ASC";
    }

    private readonly MonthLabel _monthLabel;
}
#pragma warning restore CS1591
