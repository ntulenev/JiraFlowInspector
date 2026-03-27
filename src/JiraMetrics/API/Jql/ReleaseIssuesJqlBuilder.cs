using JiraMetrics.Models.Configuration;
using JiraMetrics.Models;
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

    public JqlQuery BuildQuery(ReleaseIssueReadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var escapedProject = request.ReleaseProjectKey.Value.EscapeJqlString();
        var escapedLabel = request.ProjectLabel.Value.EscapeJqlString();
        var escapedFieldName = request.ReleaseDateFieldName.Value.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\"",
            $"labels = \"{escapedLabel}\"",
            $"\"{escapedFieldName}\" >= \"{periodStart:yyyy-MM-dd}\"",
            $"\"{escapedFieldName}\" < \"{periodEndExclusive:yyyy-MM-dd}\""
        };

        if (request.EnvironmentFilter is { } environmentFilter)
        {
            var escapedEnvironmentFieldName = environmentFilter.FieldName.Value.EscapeJqlString();
            var escapedEnvironmentFieldValue = environmentFilter.Value.Value.EscapeJqlString();
            clauses.Add($"\"{escapedEnvironmentFieldName}\" = \"{escapedEnvironmentFieldValue}\"");
        }

        return new JqlQuery($"{string.Join(" AND ", clauses)} ORDER BY \"{escapedFieldName}\" ASC, key ASC");
    }

    private readonly ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591

