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
        _reportPeriod = settings.Value.ReportPeriod;
    }

    public string BuildQuery(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? environmentFieldName,
        string? environmentFieldValue)
    {
        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var escapedProject = releaseProjectKey.Value.EscapeJqlString();
        var escapedLabel = projectLabel.EscapeJqlString();
        var escapedFieldName = releaseDateFieldName.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\"",
            $"labels = \"{escapedLabel}\"",
            $"\"{escapedFieldName}\" >= \"{periodStart:yyyy-MM-dd}\"",
            $"\"{escapedFieldName}\" < \"{periodEndExclusive:yyyy-MM-dd}\""
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

    private readonly ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591
