using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Aggregated data used by report generation.
/// </summary>
public sealed class JiraReportData
{
    /// <summary>
    /// Creates aggregated report data when transition analysis is unavailable,
    /// while keeping optional sections such as release and ratio reports.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="reportContext">Preloaded report context.</param>
    /// <param name="allTasksRatio">All-tasks ratio snapshot.</param>
    /// <param name="bugRatio">Bug ratio snapshot.</param>
    /// <param name="internalIncidents">Internal incidents snapshot.</param>
    /// <param name="testCoverage">Automated test coverage snapshot.</param>
    /// <param name="failures">Issue load failures.</param>
    /// <param name="successfulCount">Count of successfully loaded issues for the main analysis set.</param>
    /// <param name="matchedStageCount">Count of issues that matched transition-analysis prerequisites.</param>
    /// <returns>Aggregated report data.</returns>
    public static JiraReportData CreateWithoutTransitionAnalysis(
        AppSettings settings,
        JiraReportContext reportContext,
        IssueRatioSnapshot allTasksRatio,
        IssueRatioSnapshot? bugRatio,
        IssueRatioSnapshot? internalIncidents,
        TestCoverageSnapshot testCoverage,
        IReadOnlyList<LoadFailure> failures,
        ItemCount successfulCount,
        ItemCount matchedStageCount)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(reportContext);
        ArgumentNullException.ThrowIfNull(allTasksRatio);
        ArgumentNullException.ThrowIfNull(failures);

        return CreateCore(
            settings,
            reportContext,
            allTasksRatio,
            bugRatio,
            internalIncidents,
            testCoverage,
            doneIssues: [],
            doneDaysAtWork75PerType: [],
            customTransitionIssues: [],
            customTransitionDuration75PerType: [],
            qaTransitionAnalysis: QaTransitionAnalysis.Empty,
            rejectedIssues: [],
            pathSummary: new PathGroupsSummary(
                successfulCount,
                matchedStageCount,
                new ItemCount(failures.Count),
                new ItemCount(0)),
            pathGroups: [],
            failures);
    }

    /// <summary>
    /// Creates aggregated report data for a successful analysis run.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="reportContext">Preloaded report context.</param>
    /// <param name="allTasksRatio">All-tasks ratio snapshot.</param>
    /// <param name="bugRatio">Bug ratio snapshot.</param>
    /// <param name="internalIncidents">Internal incidents snapshot.</param>
    /// <param name="testCoverage">Automated test coverage snapshot.</param>
    /// <param name="analysis">Issue analysis result.</param>
    /// <param name="failures">Issue load failures.</param>
    /// <returns>Aggregated report data.</returns>
    public static JiraReportData Create(
        AppSettings settings,
        JiraReportContext reportContext,
        IssueRatioSnapshot allTasksRatio,
        IssueRatioSnapshot? bugRatio,
        IssueRatioSnapshot? internalIncidents,
        TestCoverageSnapshot testCoverage,
        JiraIssueAnalysisResult analysis,
        IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(reportContext);
        ArgumentNullException.ThrowIfNull(allTasksRatio);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(failures);

        if (analysis.Outcome != JiraIssueAnalysisOutcome.Success)
        {
            throw new InvalidOperationException(
                "Report data can only be created for a successful analysis.");
        }

        if (analysis.PathSummary is null)
        {
            throw new InvalidOperationException(
                "Successful analysis must include path summary data.");
        }

        return CreateCore(
            settings,
            reportContext,
            allTasksRatio,
            bugRatio,
            internalIncidents,
            testCoverage,
            analysis.DoneIssues,
            analysis.DoneDaysAtWork75PerType,
            analysis.CustomTransitionIssues,
            analysis.CustomTransitionDuration75PerType,
            analysis.QaTransitionAnalysis,
            analysis.RejectedIssues,
            analysis.PathSummary,
            analysis.PathGroups,
            failures);
    }

    private static JiraReportData CreateCore(
        AppSettings settings,
        JiraReportContext reportContext,
        IssueRatioSnapshot allTasksRatio,
        IssueRatioSnapshot? bugRatio,
        IssueRatioSnapshot? internalIncidents,
        TestCoverageSnapshot testCoverage,
        IReadOnlyList<IssueTimeline> doneIssues,
        IReadOnlyList<IssueTypeWorkDays75Summary> doneDaysAtWork75PerType,
        IReadOnlyList<CustomTransitionIssue> customTransitionIssues,
        IReadOnlyList<IssueTypeDuration75Summary> customTransitionDuration75PerType,
        QaTransitionAnalysis qaTransitionAnalysis,
        IReadOnlyList<IssueTimeline> rejectedIssues,
        PathGroupsSummary pathSummary,
        IReadOnlyList<PathGroup> pathGroups,
        IReadOnlyList<LoadFailure> failures) =>
        new()
        {
            Settings = settings,
            Source = new JiraReportSourceData
            {
                SearchIssueCount = new ItemCount(reportContext.IssueKeys.Count),
                ReleaseIssues = reportContext.ReleaseIssues,
                ArchTasks = reportContext.ArchTasks,
                GlobalIncidents = reportContext.GlobalIncidents,
                Unresolved30DaysTasks = reportContext.Unresolved30DaysTasks,
                RoadmapItems = reportContext.RoadmapItems,
                OpenIssuesByStatus = reportContext.OpenIssuesByStatus
            },
            Ratios = new JiraReportRatioData
            {
                AllTasks = allTasksRatio,
                Bugs = bugRatio,
                InternalIncidents = internalIncidents,
                TestCoverage = testCoverage
            },
            Transitions = new JiraReportTransitionData
            {
                DoneIssues = doneIssues,
                DoneDaysAtWork75PerType = doneDaysAtWork75PerType,
                CustomTransitionIssues = customTransitionIssues,
                CustomTransitionDuration75PerType = customTransitionDuration75PerType,
                QaTransitionAnalysis = qaTransitionAnalysis,
                RejectedIssues = rejectedIssues,
                PathSummary = pathSummary,
                PathGroups = pathGroups
            },
            Failures = failures
        };

    /// <summary>
    /// Gets or sets application settings.
    /// </summary>
    public required AppSettings Settings { get; init; }

    /// <summary>
    /// Gets the source datasets loaded before report analysis.
    /// </summary>
    public JiraReportSourceData Source { get; init; } = new();

    /// <summary>
    /// Gets ratio snapshots and automated test coverage.
    /// </summary>
    public JiraReportRatioData Ratios { get; init; } = new();

    /// <summary>
    /// Gets transition analysis results.
    /// </summary>
    public JiraReportTransitionData Transitions { get; init; } = new();

    /// <summary>
    /// Gets or sets failed issue loads.
    /// </summary>
    public IReadOnlyList<LoadFailure> Failures { get; init; } = [];
}
