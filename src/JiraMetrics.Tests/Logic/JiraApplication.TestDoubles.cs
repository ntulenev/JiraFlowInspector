using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed partial class JiraApplicationTests
{
    private sealed class FakeApiClient :
        IJiraUserClient,
        IJiraIssueSearchClient,
        IJiraReportDataClient,
        IJiraIssueTimelineClient
    {
        public JiraAuthUser CurrentUser { get; set; } = new(new UserDisplayName("unknown"), null, null);

        public IReadOnlyList<IssueKey> IssueKeys { get; set; } = [];

        public IReadOnlyList<IssueKey> RejectIssueKeys { get; set; } = [];

        public HashSet<IssueKey> FailIssueKeys { get; set; } = [];

        public IssueTimeline? IssueToReturn { get; set; }

        public Dictionary<string, IssueTimeline> IssuesByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool ThrowOnAuth { get; set; }

        public IReadOnlyList<IssueListItem> CreatedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> MovedToDoneThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> RejectedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus { get; set; } = [];

        public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; set; } = [];

        public IReadOnlyList<ArchTaskItem> ArchTasks { get; set; } = [];

        public IReadOnlyList<GlobalIncidentItem> GlobalIncidents { get; set; } = [];

        public bool CreatedThisMonthIssuesRequested { get; private set; }

        public bool MovedToDoneThisMonthIssuesRequested { get; private set; }

        public bool RejectedThisMonthIssuesRequested { get; private set; }

        public int IssueKeysMovedToDoneThisMonthRequestCount { get; private set; }

        public int MovedToDoneThisMonthIssuesRequestCount { get; private set; }

        public int RejectedThisMonthIssuesRequestCount { get; private set; }

        public bool OpenIssuesByStatusRequested { get; private set; }

        public bool ReleaseIssuesRequested { get; private set; }

        public bool ArchTasksRequested { get; private set; }

        public bool GlobalIncidentsRequested { get; private set; }

        public int IssueTimelinesRequestCount { get; private set; }

        public int SingleIssueTimelineRequestCount { get; private set; }

        public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnAuth)
            {
                throw new InvalidOperationException("Auth failed.");
            }

            return Task.FromResult(CurrentUser);
        }

        public Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            CreatedAfterDate? createdAfter,
            CancellationToken cancellationToken)
        {
            IssueKeysMovedToDoneThisMonthRequestCount++;
            return Task.FromResult(
                string.Equals(doneStatusName.Value, "Reject", StringComparison.OrdinalIgnoreCase)
                    ? (RejectIssueKeys.Count == 0 ? IssueKeys : RejectIssueKeys)
                    : IssueKeys);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null)
        {
            CreatedThisMonthIssuesRequested = true;
            return Task.FromResult(CreatedThisMonthIssues);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null,
            bool includeIssueLinks = false)
        {
            if (string.Equals(doneStatusName.Value, "Reject", StringComparison.OrdinalIgnoreCase))
            {
                RejectedThisMonthIssuesRequested = true;
                RejectedThisMonthIssuesRequestCount++;
                return Task.FromResult<IReadOnlyList<IssueListItem>>(
                    RejectedThisMonthIssues.Count > 0
                        ? RejectedThisMonthIssues
                        : [.. (RejectIssueKeys.Count == 0 ? IssueKeys : RejectIssueKeys)
                            .Select(static key => new IssueListItem(key, new IssueSummary($"Summary {key.Value}")))]
                );
            }

            MovedToDoneThisMonthIssuesRequested = true;
            MovedToDoneThisMonthIssuesRequestCount++;
            return Task.FromResult<IReadOnlyList<IssueListItem>>(
                MovedToDoneThisMonthIssues.Count > 0
                    ? MovedToDoneThisMonthIssues
                    : [.. IssueKeys.Select(static key => new IssueListItem(key, new IssueSummary($"Summary {key.Value}")))]
            );
        }

        public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
            ReleaseIssueReadRequest request,
            CancellationToken cancellationToken)
        {
            ReleaseIssuesRequested = true;
            return Task.FromResult(ReleaseIssues);
        }

        public Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
            ArchTasksReportSettings settings,
            CancellationToken cancellationToken)
        {
            ArchTasksRequested = true;
            return Task.FromResult(ArchTasks);
        }

        public Task<IReadOnlyList<IssueListItem>> GetUnresolved30DaysTasksAsync(
            Unresolved30DaysTasksReportSettings settings,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssueListItem>>([]);

        public Task<IReadOnlyList<RoadmapItem>> GetRoadmapItemsAsync(
            RoadmapReportSettings settings,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoadmapItem>>([]);

        public Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
            GlobalIncidentsReportSettings settings,
            CancellationToken cancellationToken)
        {
            GlobalIncidentsRequested = true;
            return Task.FromResult(GlobalIncidents);
        }

        public Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            StatusName? rejectStatusName,
            CancellationToken cancellationToken)
        {
            OpenIssuesByStatusRequested = true;
            return Task.FromResult(OpenIssuesByStatus);
        }

        public Task<IssueTimelineBatchResult> GetIssueTimelinesAsync(
            IReadOnlyList<IssueKey> issueKeys,
            CancellationToken cancellationToken)
        {
            IssueTimelinesRequestCount++;

            var issues = new List<IssueTimeline>(issueKeys.Count);
            var failures = new List<LoadFailure>();

            foreach (var issueKey in issueKeys)
            {
                if (FailIssueKeys.Contains(issueKey))
                {
                    failures.Add(new LoadFailure(issueKey, new ErrorMessage("Failed to load issue.")));
                    continue;
                }

                if (IssuesByKey.TryGetValue(issueKey.Value, out var configuredIssue))
                {
                    issues.Add(new IssueTimeline(
                        issueKey,
                        configuredIssue.IssueType,
                        configuredIssue.Summary,
                        configuredIssue.Created,
                        configuredIssue.EndTime,
                        configuredIssue.Transitions,
                        configuredIssue.PathKey,
                        configuredIssue.PathLabel,
                        configuredIssue.SubItemsCount,
                        configuredIssue.HasPullRequest));
                    continue;
                }

                if (IssueToReturn is null)
                {
                    failures.Add(new LoadFailure(issueKey, new ErrorMessage("No issue configured for fake transport.")));
                    continue;
                }

                issues.Add(new IssueTimeline(
                    issueKey,
                    IssueToReturn.IssueType,
                    IssueToReturn.Summary,
                    IssueToReturn.Created,
                    IssueToReturn.EndTime,
                    IssueToReturn.Transitions,
                    IssueToReturn.PathKey,
                    IssueToReturn.PathLabel,
                    IssueToReturn.SubItemsCount,
                    IssueToReturn.HasPullRequest));
            }

            return Task.FromResult(new IssueTimelineBatchResult(issues, failures));
        }

        public async Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken)
        {
            SingleIssueTimelineRequestCount++;

            if (FailIssueKeys.Contains(issueKey))
            {
                throw new InvalidOperationException("Failed to load issue.");
            }

            if (IssuesByKey.TryGetValue(issueKey.Value, out var configuredIssue))
            {
                return new IssueTimeline(
                    issueKey,
                    configuredIssue.IssueType,
                    configuredIssue.Summary,
                    configuredIssue.Created,
                    configuredIssue.EndTime,
                    configuredIssue.Transitions,
                    configuredIssue.PathKey,
                    configuredIssue.PathLabel,
                    configuredIssue.SubItemsCount,
                    configuredIssue.HasPullRequest);
            }

            if (IssueToReturn is null)
            {
                throw new InvalidOperationException("No issue configured for fake transport.");
            }

            return await Task.FromResult(new IssueTimeline(
                issueKey,
                IssueToReturn.IssueType,
                IssueToReturn.Summary,
                IssueToReturn.Created,
                IssueToReturn.EndTime,
                IssueToReturn.Transitions,
                IssueToReturn.PathKey,
                IssueToReturn.PathLabel,
                IssueToReturn.SubItemsCount,
                IssueToReturn.HasPullRequest));
        }
    }

    private sealed class FakeRequestTelemetryCollector : IJiraRequestTelemetryCollector
    {
        public JiraRequestTelemetrySummary Summary { get; set; } = new(0, 0, 0, TimeSpan.Zero, []);

        public void Reset()
        {
        }

        public void Record(string method, Uri url, TimeSpan duration, int responseBytes, bool isRetry)
        {
        }

        public JiraRequestTelemetrySummary GetSummary() => Summary;
    }

    private sealed class NoOpReportLoader : IJiraApplicationReportLoader
    {
        public Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new JiraAuthUser(new UserDisplayName("Test"), "user@example.com", "1"));

        public Task<ReportLoadResult> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<ReportLoadResult>(new ReportLoadResult.Failure(new ErrorMessage("Report load failed.")));
    }

    private sealed class NoOpReportPresenter : IJiraApplicationReportPresenter
    {
        public void ShowLoadingStarted()
        {
        }

        public void ShowLoaded(JiraApplicationReportData reportData)
        {
        }
    }

    private sealed class NoOpAnalysisRunner : IJiraApplicationAnalysisRunner
    {
        public Task RunAsync(JiraApplicationReportData reportData, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakePresentationService :
        IJiraPresentationService,
        IJiraReportPipeline
    {
        public List<string> Calls { get; } = [];

        public bool AuthenticationFailedShown { get; private set; }

        public bool NoIssuesMatchedFilterShown { get; private set; }

        public bool DoneIssuesTableShown { get; private set; }

        public bool RejectedIssuesTableShown { get; private set; }

        public bool FailuresShown { get; private set; }

        public bool BugRatioShown { get; private set; }

        public bool TestCoverageShown { get; private set; }

        public bool TestCoverageLoadingStartedShown { get; private set; }

        public bool BugRatioLoadingStartedShown { get; private set; }

        public bool BugRatioLoadingCompletedShown { get; private set; }

        public bool AllTasksRatioShown { get; private set; }

        public bool AllTasksRatioLoadingStartedShown { get; private set; }

        public bool AllTasksRatioLoadingCompletedShown { get; private set; }

        public bool ReleaseReportShown { get; private set; }

        public bool ReleaseReportLoadingStartedShown { get; private set; }

        public bool ArchTasksReportShown { get; private set; }

        public bool ArchTasksReportLoadingStartedShown { get; private set; }

        public bool GlobalIncidentsReportShown { get; private set; }

        public bool GlobalIncidentsReportLoadingStartedShown { get; private set; }

        public bool OpenIssuesByStatusShown { get; private set; }

        public bool DoneDaysAtWork75PerTypeShown { get; private set; }

        public ItemCount? IssueLoadingStartedTotal { get; private set; }

        public ItemCount? IssueLoadingCompletedLoaded { get; private set; }

        public List<string> ProcessingSteps { get; } = [];

        public IReadOnlyList<IssueTimeline> DoneIssues { get; private set; } = [];

        public IReadOnlyList<IssueTimeline> RejectedIssues { get; private set; } = [];

        public PathGroupsSummary? PathGroupsSummary { get; private set; }

        public bool ExecutionSummaryShown { get; private set; }

        public bool ReportRendered { get; private set; }

        public JiraReportData? LastReportData { get; private set; }

        public void ShowAuthenticationStarted()
        {
        }

        public void ShowAuthenticationSucceeded(JiraAuthUser user)
        {
        }

        public void ShowAuthenticationFailed(ErrorMessage errorMessage) => AuthenticationFailedShown = true;

        public void ShowIssueSearchFailed(ErrorMessage errorMessage)
        {
        }

        public void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter) => Calls.Add("ReportPeriodContext");

        public void ShowReportHeader(AppSettings settings, ItemCount issueCount) => Calls.Add("ReportHeader");

        public void ShowNoIssuesMatchedFilter() => NoIssuesMatchedFilterShown = true;

        public void ShowIssueLoadingStarted(ItemCount totalIssues)
        {
            IssueLoadingStartedTotal = totalIssues;
        }

        public void ShowIssueLoaded(IssueKey issueKey)
        {
        }

        public void ShowIssueFailed(IssueKey issueKey)
        {
        }

        public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues)
        {
            IssueLoadingCompletedLoaded = loadedIssues;
        }

        public void ShowProcessingStep(string message)
        {
            ProcessingSteps.Add(message);
        }

        public void ShowSpacer()
        {
        }

        public void ShowNoIssuesLoaded()
        {
        }

        public void ShowNoIssuesMatchedRequiredStage()
        {
        }

        public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
        {
            DoneIssues = [.. issues];
            DoneIssuesTableShown = issues.Count > 0;
            Calls.Add("DoneIssuesTable");
        }

        public void ShowDoneDaysAtWork75PerType(
            IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
            StatusName doneStatusName)
        {
            DoneDaysAtWork75PerTypeShown = true;
            Calls.Add("DoneDaysAtWork75PerType");
        }

        public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
        {
            RejectedIssues = [.. issues];
            if (issues.Count > 0)
            {
                RejectedIssuesTableShown = true;
            }
        }

        public void ShowPathGroupsSummary(PathGroupsSummary summary)
        {
            PathGroupsSummary = summary;
        }

        public void ShowReleaseReport(
            ReleaseReportSettings settings,
            ReportPeriod reportPeriod,
            IReadOnlyList<ReleaseIssueItem> releases)
        {
            ReleaseReportShown = true;
            Calls.Add("ReleaseReport");
        }

        public void ShowReleaseReportLoadingStarted()
        {
            ReleaseReportLoadingStartedShown = true;
            Calls.Add("ReleaseReportLoadingStarted");
        }

        public void ShowArchTasksReportLoadingStarted()
        {
            ArchTasksReportLoadingStartedShown = true;
            Calls.Add("ArchTasksReportLoadingStarted");
        }

        public void ShowArchTasksReport(
            ArchTasksReportSettings settings,
            IReadOnlyList<ArchTaskItem> tasks)
        {
            ArchTasksReportShown = true;
            Calls.Add("ArchTasksReport");
        }

        public void ShowGlobalIncidentsReportLoadingStarted()
        {
            GlobalIncidentsReportLoadingStartedShown = true;
            Calls.Add("GlobalIncidentsReportLoadingStarted");
        }

        public void ShowGlobalIncidentsReport(
            GlobalIncidentsReportSettings settings,
            ReportPeriod reportPeriod,
            IReadOnlyList<GlobalIncidentItem> incidents)
        {
            GlobalIncidentsReportShown = true;
            Calls.Add("GlobalIncidentsReport");
        }

        public void ShowAllTasksRatioLoadingStarted()
        {
            AllTasksRatioLoadingStartedShown = true;
            Calls.Add("AllTasksRatioLoadingStarted");
        }

        public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
        {
            AllTasksRatioLoadingCompletedShown = true;
            Calls.Add("AllTasksRatioLoadingCompleted");
        }

        public void ShowAllTasksRatio(
            string? customFieldName,
            string? customFieldValue,
            IssueRatioSnapshot snapshot)
        {
            AllTasksRatioShown = true;
            Calls.Add("AllTasksRatio");
        }

        public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
        {
            BugRatioLoadingStartedShown = true;
            Calls.Add("BugRatioLoadingStarted");
        }

        public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
        {
            BugRatioLoadingCompletedShown = true;
            Calls.Add("BugRatioLoadingCompleted");
        }

        public void ShowBugRatio(
            IReadOnlyList<IssueTypeName> bugIssueNames,
            string? customFieldName,
            string? customFieldValue,
            IssueRatioSnapshot snapshot)
        {
            BugRatioShown = true;
            Calls.Add("BugRatio");
        }

        public void ShowTestCoverageLoadingStarted(TestCoverageSettings settings)
        {
            TestCoverageLoadingStartedShown = true;
            Calls.Add("TestCoverageLoadingStarted");
        }

        public void ShowTestCoverage(TestCoverageSettings settings, TestCoverageSnapshot snapshot)
        {
            TestCoverageShown = true;
            Calls.Add("TestCoverage");
        }

        public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
        {
            Calls.Add("PathGroups");
        }

        public void ShowOpenIssuesByStatusSummary(
            IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
            StatusName doneStatusName,
            StatusName? rejectStatusName)
        {
            OpenIssuesByStatusShown = true;
            Calls.Add("OpenIssuesByStatusSummary");
        }

        public void ShowFailures(IReadOnlyList<LoadFailure> failures)
        {
            if (failures.Count > 0)
            {
                FailuresShown = true;
            }
        }

        public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
        {
            ExecutionSummaryShown = true;
            Calls.Add("ExecutionSummary");
        }

        public void RenderReport(JiraReportData reportData)
        {
            ReportRendered = true;
            LastReportData = reportData;
        }
    }
}
