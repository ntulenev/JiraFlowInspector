using JiraMetrics.Helpers;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated architecture tasks report settings.
/// </summary>
public sealed record ArchTasksReportSettings
{
    private const string MONTH_LABEL_TOKEN = "{{MonthLabel}}";
    private const string MONTH_RESOLVED_CLAUSE_TOKEN = "{{MonthResolvedClause}}";
    private const string LEGACY_MONTH_RESOLVED_CLAUSE_TOKEN = "resolved in MonthLabel";
    private const string LEGACY_MONTH_RESOLVED_CLAUSE_TOKEN_RU = "resolved попадает в MonthLabel";

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchTasksReportSettings"/> class.
    /// </summary>
    /// <param name="jql">Raw JQL or JQL template used to load architecture tasks.</param>
    public ArchTasksReportSettings(string jql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jql);
        Jql = jql.Trim();
    }

    /// <summary>
    /// Gets raw JQL or JQL template used to load architecture tasks.
    /// </summary>
    public string Jql { get; }

    /// <summary>
    /// Builds final JQL for a specific reporting month.
    /// </summary>
    /// <param name="monthLabel">Reporting month.</param>
    /// <returns>Resolved JQL string.</returns>
    public string BuildJql(MonthLabel monthLabel)
    {
        var (monthStart, nextMonthStart) = monthLabel.GetMonthRange();
        var monthResolvedClause =
            $"(resolved >= \"{monthStart:yyyy-MM-dd}\" AND resolved < \"{nextMonthStart:yyyy-MM-dd}\")";

        return Jql
            .Replace(MONTH_RESOLVED_CLAUSE_TOKEN, monthResolvedClause, StringComparison.OrdinalIgnoreCase)
            .Replace(LEGACY_MONTH_RESOLVED_CLAUSE_TOKEN_RU, monthResolvedClause, StringComparison.OrdinalIgnoreCase)
            .Replace(LEGACY_MONTH_RESOLVED_CLAUSE_TOKEN, monthResolvedClause, StringComparison.OrdinalIgnoreCase)
            .Replace(MONTH_LABEL_TOKEN, monthLabel.Value.EscapeJqlString(), StringComparison.OrdinalIgnoreCase);
    }
}
