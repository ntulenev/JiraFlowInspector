using JiraMetrics.Abstractions;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.Helpers;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Builds JQL for global incident queries.
/// </summary>
public sealed class GlobalIncidentsJqlBuilder : IGlobalIncidentsJqlBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalIncidentsJqlBuilder"/> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public GlobalIncidentsJqlBuilder(IOptions<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _reportPeriod = settings.Value.ReportPeriod;
    }

    public string BuildQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var escapedNamespace = settings.Namespace.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedNamespace}\"",
            BuildDateRangeClause(incidentStartFields, periodStart, periodEndExclusive)
        };

        if (!string.IsNullOrWhiteSpace(settings.JqlFilter))
        {
            clauses.Add($"({settings.JqlFilter.Trim()})");
        }
        else
        {
            AddTextSearchClauses(clauses, settings.SearchPhrase);
        }

        return $"{string.Join(" AND ", clauses)} ORDER BY key ASC";
    }

    private static string BuildDateRangeClause(
        IReadOnlyList<ResolvedJiraField> fields,
        DateOnly periodStart,
        DateOnly periodEndExclusive)
    {
        var fieldClauses = fields
            .Select(field =>
            {
                var escapedField = field.FieldName.EscapeJqlString();
                return
                    $"(\"{escapedField}\" >= \"{periodStart:yyyy-MM-dd}\""
                    + $" AND \"{escapedField}\" < \"{periodEndExclusive:yyyy-MM-dd}\")";
            })
            .ToArray();

        return fieldClauses.Length == 1
            ? fieldClauses[0]
            : $"({string.Join(" OR ", fieldClauses)})";
    }

    private static void AddTextSearchClauses(List<string> clauses, string? searchPhrase)
    {
        if (string.IsNullOrWhiteSpace(searchPhrase))
        {
            return;
        }

        var terms = searchPhrase
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static term => term.Trim().Trim('"', '\''))
            .Where(static term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var term in terms)
        {
            var escapedTerm = term.EscapeJqlString();
            var pattern = escapedTerm.EndsWith('*') ? escapedTerm : escapedTerm + "*";
            clauses.Add($"text ~ \"{pattern}\"");
        }
    }

    private readonly ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591
