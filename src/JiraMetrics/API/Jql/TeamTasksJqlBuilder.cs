using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Helpers;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Builds JQL queries for team task reports.
/// </summary>
public sealed class TeamTasksJqlBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamTasksJqlBuilder"/> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public TeamTasksJqlBuilder(IOptions<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var resolved = settings.Value;
        _customFieldName = string.IsNullOrWhiteSpace(resolved.CustomFieldName)
            ? null
            : resolved.CustomFieldName.Trim();
        _customFieldValue = string.IsNullOrWhiteSpace(resolved.CustomFieldValue)
            ? null
            : resolved.CustomFieldValue.Trim();
        _reportPeriod = resolved.ReportPeriod;
    }

    /// <summary>
    /// Builds a query for issue keys moved to the done status during the report period.
    /// </summary>
    /// <param name="projectKey">Jira project key.</param>
    /// <param name="doneStatusName">Done status name.</param>
    /// <param name="createdAfter">Optional lower bound for the issue creation date.</param>
    /// <returns>The JQL query.</returns>
    public JqlQuery BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter)
    {
        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildMovedToDoneClause(doneStatusName, periodStart, periodEndExclusive));

        if (createdAfter is { } createdAfterDate)
        {
            clauses.Add($"created >= \"{createdAfterDate}\"");
        }

        return new JqlQuery($"{string.Join(" AND ", clauses)} ORDER BY key ASC");
    }

    /// <summary>
    /// Builds a query for issues created during the report period.
    /// </summary>
    /// <param name="projectKey">Jira project key.</param>
    /// <param name="issueTypes">Optional issue types to include.</param>
    /// <returns>The JQL query.</returns>
    public JqlQuery BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add($"created >= \"{periodStart:yyyy-MM-dd}\"");
        clauses.Add($"created < \"{periodEndExclusive:yyyy-MM-dd}\"");
        AddIssueTypesClause(clauses, issueTypes);

        return new JqlQuery($"{string.Join(" AND ", clauses)} ORDER BY key ASC");
    }

    /// <summary>
    /// Builds a query for issues moved to the done status during the report period.
    /// </summary>
    /// <param name="projectKey">Jira project key.</param>
    /// <param name="doneStatusName">Done status name.</param>
    /// <param name="issueTypes">Optional issue types to include.</param>
    /// <returns>The JQL query.</returns>
    public JqlQuery BuildMovedToDoneIssuesQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes)
    {
        ArgumentNullException.ThrowIfNull(issueTypes);

        var periodStart = _reportPeriod.Start;
        var periodEndExclusive = _reportPeriod.EndExclusive;
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildMovedToDoneClause(doneStatusName, periodStart, periodEndExclusive));
        AddIssueTypesClause(clauses, issueTypes);

        return new JqlQuery($"{string.Join(" AND ", clauses)} ORDER BY key ASC");
    }

    /// <summary>
    /// Builds a query for open issue counts grouped by status and issue type.
    /// </summary>
    /// <param name="projectKey">Jira project key.</param>
    /// <param name="doneStatusName">Done status to exclude.</param>
    /// <param name="rejectStatusName">Optional rejected status to exclude.</param>
    /// <returns>The JQL query.</returns>
    public JqlQuery BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        var clauses = BuildProjectClauses(projectKey);
        clauses.Add(BuildExcludedStatusesClause(doneStatusName, rejectStatusName));

        return new JqlQuery($"{string.Join(" AND ", clauses)} ORDER BY status ASC, key ASC");
    }

    private List<string> BuildProjectClauses(ProjectKey projectKey)
    {
        var escapedProject = projectKey.Value.EscapeJqlString();
        var clauses = new List<string>
        {
            $"project = \"{escapedProject}\""
        };

        if (!string.IsNullOrWhiteSpace(_customFieldName) && !string.IsNullOrWhiteSpace(_customFieldValue))
        {
            var escapedName = _customFieldName.EscapeJqlString();
            var escapedValue = _customFieldValue.EscapeJqlString();
            clauses.Add($"\"{escapedName}\" = \"{escapedValue}\"");
        }

        return clauses;
    }

    private static void AddIssueTypesClause(List<string> clauses, IReadOnlyList<IssueTypeName> issueTypes)
    {
        if (issueTypes.Count == 0)
        {
            return;
        }

        var escapedIssueTypes = issueTypes
            .Select(static issueType => $"\"{issueType.Value.EscapeJqlString()}\"")
            .ToArray();
        var issueTypeClause = escapedIssueTypes.Length == 1
            ? $"issuetype = {escapedIssueTypes[0]}"
            : $"issuetype IN ({string.Join(", ", escapedIssueTypes)})";

        clauses.Add(issueTypeClause);
    }

    private static string BuildMovedToDoneClause(
        StatusName doneStatusName,
        DateOnly periodStart,
        DateOnly periodEndExclusive)
    {
        var escapedDoneStatus = doneStatusName.Value.EscapeJqlString();
        return
            $"status CHANGED TO \"{escapedDoneStatus}\" AFTER \"{periodStart:yyyy-MM-dd}\" "
            + $"BEFORE \"{periodEndExclusive:yyyy-MM-dd}\" AND status = \"{escapedDoneStatus}\"";
    }

    private static string BuildExcludedStatusesClause(StatusName doneStatusName, StatusName? rejectStatusName)
    {
        var statusesToExclude = new List<string>
        {
            $"\"{doneStatusName.Value.EscapeJqlString()}\""
        };

        if (rejectStatusName is { } rejectStatus
            && !string.Equals(doneStatusName.Value, rejectStatus.Value, StringComparison.OrdinalIgnoreCase))
        {
            statusesToExclude.Add($"\"{rejectStatus.Value.EscapeJqlString()}\"");
        }

        return statusesToExclude.Count == 1
            ? $"status != {statusesToExclude[0]}"
            : $"status NOT IN ({string.Join(", ", statusesToExclude)})";
    }

    private readonly string? _customFieldName;
    private readonly string? _customFieldValue;
    private readonly ReportPeriod _reportPeriod;
}
#pragma warning restore CS1591

