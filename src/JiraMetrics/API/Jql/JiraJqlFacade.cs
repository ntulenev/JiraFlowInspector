using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591
namespace JiraMetrics.API.Jql;

/// <summary>
/// Facade over the specialized Jira JQL builders.
/// </summary>
public sealed class JiraJqlFacade : IJiraJqlFacade
{
    public JiraJqlFacade(
        ITeamTasksJqlBuilder teamTasksJqlBuilder,
        IReleaseIssuesJqlBuilder releaseIssuesJqlBuilder,
        IArchTasksJqlBuilder archTasksJqlBuilder,
        IGlobalIncidentsJqlBuilder globalIncidentsJqlBuilder)
    {
        ArgumentNullException.ThrowIfNull(teamTasksJqlBuilder);
        ArgumentNullException.ThrowIfNull(releaseIssuesJqlBuilder);
        ArgumentNullException.ThrowIfNull(archTasksJqlBuilder);
        ArgumentNullException.ThrowIfNull(globalIncidentsJqlBuilder);

        _teamTasksJqlBuilder = teamTasksJqlBuilder;
        _releaseIssuesJqlBuilder = releaseIssuesJqlBuilder;
        _archTasksJqlBuilder = archTasksJqlBuilder;
        _globalIncidentsJqlBuilder = globalIncidentsJqlBuilder;
    }

    public JqlQuery BuildMovedToDoneIssueKeysQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        CreatedAfterDate? createdAfter) =>
        _teamTasksJqlBuilder.BuildMovedToDoneIssueKeysQuery(projectKey, doneStatusName, createdAfter);

    public JqlQuery BuildCreatedIssuesQuery(ProjectKey projectKey, IReadOnlyList<IssueTypeName> issueTypes) =>
        _teamTasksJqlBuilder.BuildCreatedIssuesQuery(projectKey, issueTypes);

    public JqlQuery BuildMovedToDoneIssuesQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        IReadOnlyList<IssueTypeName> issueTypes) =>
        _teamTasksJqlBuilder.BuildMovedToDoneIssuesQuery(projectKey, doneStatusName, issueTypes);

    public JqlQuery BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
        ProjectKey projectKey,
        StatusName doneStatusName,
        StatusName? rejectStatusName) =>
        _teamTasksJqlBuilder.BuildIssueCountsByStatusExcludingDoneAndRejectQuery(
            projectKey,
            doneStatusName,
            rejectStatusName);

    public JqlQuery BuildReleaseIssuesQuery(ReleaseIssueReadRequest request) =>
        _releaseIssuesJqlBuilder.BuildQuery(request);

    public JqlQuery BuildArchTasksQuery(ArchTasksReportSettings settings) =>
        _archTasksJqlBuilder.BuildQuery(settings);

    public JqlQuery BuildGlobalIncidentsQuery(
        GlobalIncidentsReportSettings settings,
        IReadOnlyList<ResolvedJiraField> incidentStartFields) =>
        _globalIncidentsJqlBuilder.BuildQuery(settings, incidentStartFields);

    private readonly ITeamTasksJqlBuilder _teamTasksJqlBuilder;
    private readonly IReleaseIssuesJqlBuilder _releaseIssuesJqlBuilder;
    private readonly IArchTasksJqlBuilder _archTasksJqlBuilder;
    private readonly IGlobalIncidentsJqlBuilder _globalIncidentsJqlBuilder;
}
#pragma warning restore CS1591

