using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Aggregated data used by PDF report generation.
/// </summary>
public sealed class JiraPdfReportData
{
    /// <summary>
    /// Creates aggregated PDF report data for a successful analysis run.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="reportContext">Preloaded report context.</param>
    /// <param name="allTasksRatio">All-tasks ratio snapshot.</param>
    /// <param name="bugRatio">Bug ratio snapshot.</param>
    /// <param name="analysis">Issue analysis result.</param>
    /// <param name="failures">Issue load failures.</param>
    /// <returns>Aggregated PDF report data.</returns>
    public static JiraPdfReportData Create(
        AppSettings settings,
        JiraReportContext reportContext,
        IssueRatioSnapshot allTasksRatio,
        IssueRatioSnapshot? bugRatio,
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
                "PDF report data can only be created for a successful analysis.");
        }

        if (analysis.PathSummary is null)
        {
            throw new InvalidOperationException(
                "Successful analysis must include path summary data.");
        }

        return new JiraPdfReportData
        {
            Settings = settings,
            SearchIssueCount = new ItemCount(reportContext.IssueKeys.Count),
            ReleaseIssues = reportContext.ReleaseIssues,
            ArchTasks = reportContext.ArchTasks,
            GlobalIncidents = reportContext.GlobalIncidents,
            AllTasksCreatedThisMonth = allTasksRatio.CreatedThisMonth,
            AllTasksOpenThisMonth = allTasksRatio.OpenThisMonth,
            AllTasksMovedToDoneThisMonth = allTasksRatio.MovedToDoneThisMonth,
            AllTasksRejectedThisMonth = allTasksRatio.RejectedThisMonth,
            AllTasksFinishedThisMonth = allTasksRatio.FinishedThisMonth,
            BugCreatedThisMonth = bugRatio?.CreatedThisMonth,
            BugMovedToDoneThisMonth = bugRatio?.MovedToDoneThisMonth,
            BugRejectedThisMonth = bugRatio?.RejectedThisMonth,
            BugFinishedThisMonth = bugRatio?.FinishedThisMonth,
            BugOpenIssues = bugRatio?.OpenIssues ?? [],
            BugDoneIssues = bugRatio?.DoneIssues ?? [],
            BugRejectedIssues = bugRatio?.RejectedIssues ?? [],
            OpenIssuesByStatus = reportContext.OpenIssuesByStatus,
            DoneIssues = analysis.DoneIssues,
            DoneDaysAtWork75PerType = analysis.DoneDaysAtWork75PerType,
            RejectedIssues = analysis.RejectedIssues,
            PathSummary = analysis.PathSummary,
            PathGroups = analysis.PathGroups,
            Failures = failures
        };
    }

    /// <summary>
    /// Gets or sets application settings.
    /// </summary>
    public required AppSettings Settings { get; init; }

    /// <summary>
    /// Gets or sets count of issues returned by search query.
    /// </summary>
    public required ItemCount SearchIssueCount { get; init; }

    /// <summary>
    /// Gets or sets release issues for the selected period.
    /// </summary>
    public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets architecture tasks for selected report query.
    /// </summary>
    public IReadOnlyList<ArchTaskItem> ArchTasks { get; init; } = [];

    /// <summary>
    /// Gets or sets incidents for the selected period.
    /// </summary>
    public IReadOnlyList<GlobalIncidentItem> GlobalIncidents { get; init; } = [];

    /// <summary>
    /// Gets or sets all-tasks count created in month.
    /// </summary>
    public ItemCount? AllTasksCreatedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets all-tasks count still open from issues created in month.
    /// </summary>
    public ItemCount? AllTasksOpenThisMonth { get; init; }

    /// <summary>
    /// Gets or sets all-tasks count moved to done in month.
    /// </summary>
    public ItemCount? AllTasksMovedToDoneThisMonth { get; init; }

    /// <summary>
    /// Gets or sets all-tasks count moved to rejected in month.
    /// </summary>
    public ItemCount? AllTasksRejectedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets finished all-tasks count in month.
    /// </summary>
    public ItemCount? AllTasksFinishedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets bug issue count created in month.
    /// </summary>
    public ItemCount? BugCreatedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets bug issue count moved to done in month.
    /// </summary>
    public ItemCount? BugMovedToDoneThisMonth { get; init; }

    /// <summary>
    /// Gets or sets bug issue count moved to rejected in month.
    /// </summary>
    public ItemCount? BugRejectedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets finished bug issue count in month.
    /// </summary>
    public ItemCount? BugFinishedThisMonth { get; init; }

    /// <summary>
    /// Gets or sets open bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugOpenIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets done bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugDoneIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets rejected bug issues.
    /// </summary>
    public IReadOnlyList<IssueListItem> BugRejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets issue counts grouped by status and issue type outside done/rejected statuses.
    /// </summary>
    public IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus { get; init; } = [];

    /// <summary>
    /// Gets or sets issues moved to done in the selected period.
    /// </summary>
    public IReadOnlyList<IssueTimeline> DoneIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets days-at-work P75 grouped by issue type for done issues.
    /// </summary>
    public IReadOnlyList<IssueTypeWorkDays75Summary> DoneDaysAtWork75PerType { get; init; } = [];

    /// <summary>
    /// Gets or sets issues moved to rejected in the selected period.
    /// </summary>
    public IReadOnlyList<IssueTimeline> RejectedIssues { get; init; } = [];

    /// <summary>
    /// Gets or sets transition path summary.
    /// </summary>
    public required PathGroupsSummary PathSummary { get; init; }

    /// <summary>
    /// Gets or sets transition path groups.
    /// </summary>
    public IReadOnlyList<PathGroup> PathGroups { get; init; } = [];

    /// <summary>
    /// Gets or sets failed issue loads.
    /// </summary>
    public IReadOnlyList<LoadFailure> Failures { get; init; } = [];
}
