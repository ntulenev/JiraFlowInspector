using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Facade over issue filtering and analysis steps used by the application workflow.
/// </summary>
internal sealed class JiraApplicationAnalysisFacade : IJiraApplicationAnalysisFacade
{

    public JiraApplicationAnalysisFacade(IJiraLogicService logicService)
    {
        ArgumentNullException.ThrowIfNull(logicService);
        _logicService = logicService;
    }

    public JiraIssueAnalysisResult Analyze(
        IReadOnlyList<IssueTimeline> issues,
        IReadOnlyList<IssueTimeline> rejectIssues,
        IReadOnlyList<LoadFailure> failures,
        AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(rejectIssues);
        ArgumentNullException.ThrowIfNull(failures);
        ArgumentNullException.ThrowIfNull(settings);

        var issuesByType = _logicService.FilterIssuesByIssueTypes(issues, settings.IssueTypes);
        if (issuesByType.Count == 0)
        {
            return JiraIssueAnalysisResult.NoIssuesMatchedTypeFilter();
        }

        var filteredIssues = _logicService.FilterIssuesByRequiredStage(
            issuesByType,
            settings.RequiredPathStages);
        if (filteredIssues.Count == 0)
        {
            return JiraIssueAnalysisResult.NoIssuesMatchedRequiredStage();
        }

        IReadOnlyList<IssueTimeline> filteredRejectedIssues = [];
        if (settings.RejectStatusName is not null)
        {
            var rejectIssuesByType = _logicService.FilterIssuesByIssueTypes(
                rejectIssues,
                settings.IssueTypes);
            filteredRejectedIssues = _logicService.FilterIssuesByRequiredStage(
                rejectIssuesByType,
                settings.RequiredPathStages);
        }

        var doneDaysAtWork75PerType = _logicService.BuildDaysAtWork75PerType(
            filteredIssues,
            settings.DoneStatusName);
        var groupedIssues = filteredIssues
            .Where(static issue => issue.HasPullRequest)
            .ToList();
        var groups = _logicService.BuildPathGroups(groupedIssues);
        var pathSummary = new PathGroupsSummary(
            new ItemCount(issues.Count),
            new ItemCount(groupedIssues.Count),
            new ItemCount(failures.Count),
            new ItemCount(groups.Count));

        return JiraIssueAnalysisResult.Success(
            filteredIssues,
            filteredRejectedIssues,
            doneDaysAtWork75PerType,
            groups,
            pathSummary);
    }
    private readonly IJiraLogicService _logicService;
}

