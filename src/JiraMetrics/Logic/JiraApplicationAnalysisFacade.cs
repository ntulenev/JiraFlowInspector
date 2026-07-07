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
        IReadOnlyList<CustomTransitionIssue> customTransitionIssues = [];
        IReadOnlyList<IssueTypeDuration75Summary> customTransitionDuration75PerType = [];
        if (settings.CustomTransitionAnalysis is { } customTransitionSettings)
        {
            customTransitionIssues = _logicService.BuildCustomTransitionIssues(
                filteredIssues,
                filteredRejectedIssues,
                customTransitionSettings.FromStatusName,
                customTransitionSettings.ToStatusName,
                customTransitionSettings.CodeOnly);
            customTransitionDuration75PerType = _logicService.BuildDuration75PerType(customTransitionIssues);
        }

        var qaTransitionAnalysis = BuildQaTransitionAnalysis(
            filteredIssues,
            filteredRejectedIssues,
            settings.QaTransitionAnalysis);

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
            customTransitionIssues,
            customTransitionDuration75PerType,
            groups,
            pathSummary,
            qaTransitionAnalysis);
    }

    private QaTransitionAnalysis BuildQaTransitionAnalysis(
        IReadOnlyList<IssueTimeline> doneIssues,
        IReadOnlyList<IssueTimeline> rejectedIssues,
        QaTransitionAnalysisSettings settings)
    {
        if (!settings.Enabled)
        {
            return QaTransitionAnalysis.Empty;
        }

        var analyzedIssues = doneIssues
            .Concat(rejectedIssues)
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Where(static issue => issue.HasPullRequest)
            .ToArray();

        var pickupIssues = _logicService.BuildTransitionMeasurementIssues(
            analyzedIssues,
            settings.PickupTransitions,
            codeOnly: true);
        var testingIssues = _logicService.BuildTransitionMeasurementIssues(
            analyzedIssues,
            settings.TestingTransitions,
            codeOnly: true);

        return new QaTransitionAnalysis(
            new ItemCount(analyzedIssues.Length),
            pickupIssues,
            _logicService.BuildDuration75(pickupIssues),
            _logicService.BuildDuration75PerType(pickupIssues),
            testingIssues,
            _logicService.BuildDuration75(testingIssues),
            _logicService.BuildDuration75PerType(testingIssues));
    }

    private readonly IJiraLogicService _logicService;
}

