using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.Logic;

public sealed partial class JiraApplicationTests
{
    [Fact(DisplayName = "Constructor throws when settings are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenReportingFacadeIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraStatusPresenter reportingFacade = null!;

        // Act
        Action act = () => _ = new JiraApplication(
            reportingFacade,
            new FakeRequestTelemetryCollector(),
            new NoOpReportLoader(),
            new NoOpReportPresenter(),
            new NoOpAnalysisRunner());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when telemetry collector is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTelemetryCollectorIsNullThrowsArgumentNullException()
    {
        // Arrange
        var presentation = new FakePresentationService();
        IJiraRequestTelemetryCollector requestTelemetryCollector = null!;

        // Act
        Action act = () => _ = new JiraApplication(
            presentation,
            requestTelemetryCollector,
            new NoOpReportLoader(),
            new NoOpReportPresenter(),
            new NoOpAnalysisRunner());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when report loader is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenReportLoaderIsNullThrowsArgumentNullException()
    {
        // Arrange
        var presentation = new FakePresentationService();
        IJiraApplicationReportLoader reportLoader = null!;

        // Act
        Action act = () => _ = new JiraApplication(
            presentation,
            new FakeRequestTelemetryCollector(),
            reportLoader,
            new NoOpReportPresenter(),
            new NoOpAnalysisRunner());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when analysis runner is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAnalysisRunnerIsNullThrowsArgumentNullException()
    {
        // Arrange
        var presentation = new FakePresentationService();
        IJiraApplicationAnalysisRunner analysisRunner = null!;

        // Act
        Action act = () => _ = new JiraApplication(
            presentation,
            new FakeRequestTelemetryCollector(),
            new NoOpReportLoader(),
            new NoOpReportPresenter(),
            analysisRunner);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RunAsync propagates cancellation and still shows execution summary")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReportLoadingIsCanceledPropagatesAndShowsExecutionSummary()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var presentation = new FakePresentationService();
        var app = new JiraApplication(
            presentation,
            new FakeRequestTelemetryCollector(),
            new CanceledReportLoader(),
            new NoOpReportPresenter(),
            new NoOpAnalysisRunner());

        // Act
        Func<Task> act = () => app.RunAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        presentation.ExecutionSummaryShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync returns report-generation failure when analysis runner reports a renderer failure")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReportGenerationFailsReturnsFailureExitCode()
    {
        // Arrange
        var presentation = new FakePresentationService();
        var app = new JiraApplication(
            presentation,
            new FakeRequestTelemetryCollector(),
            new SuccessfulReportLoader(),
            new NoOpReportPresenter(),
            new NoOpAnalysisRunner(ReportGenerationOutcome.Failed));

        // Act
        var exitCode = await app.RunAsync();

        // Assert
        exitCode.Should().Be(JiraApplicationExitCode.ReportGenerationFailed);
        presentation.ExecutionSummaryShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows no issues matched filter when search returns empty list")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenSearchReturnsEmptyListShowsNoIssuesMatchedFilter()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = []
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings()),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.DoneIssuesTableShown.Should().BeFalse();
        presentation.ExecutionSummaryShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync renders PDF even when transition search returns no issues")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenTransitionSearchReturnsNoIssuesStillRendersPdfWithOptionalSections()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [],
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))],
            CreatedThisMonthIssues =
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open bug"))
            ],
            MovedToDoneThisMonthIssues =
            [
                new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done bug"))
            ],
            RejectedThisMonthIssues = []
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                bugIssueNames: [new IssueTypeName("Bug")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "Processing",
                    "Change completion date"),
                createdAfter: new CreatedAfterDate("2026-01-01"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        var exitCode = await app.RunAsync();

        // Assert
        exitCode.Should().Be(JiraApplicationExitCode.Success);
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.ReportRendered.Should().BeTrue();
        presentation.LastReportData.Should().NotBeNull();
        presentation.LastReportData!.Source.SearchIssueCount.Should().Be(new ItemCount(0));
        presentation.LastReportData.Source.ReleaseIssues.Should().ContainSingle();
        presentation.LastReportData.Ratios.Bugs!.CreatedThisMonth.Should().Be(new ItemCount(1));
        presentation.LastReportData.Ratios.Bugs.MovedToDoneThisMonth.Should().Be(new ItemCount(1));
        presentation.LastReportData.Transitions.DoneIssues.Should().BeEmpty();
        presentation.LastReportData.Transitions.PathSummary.PathGroupCount.Should().Be(new ItemCount(0));
    }

    [Fact(DisplayName = "RunAsync shows failures when issue loading fails")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueLoadingFailsShowsFailures()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            FailIssueKeys = [new("AAA-2")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings()),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.DoneIssuesTableShown.Should().BeTrue();
        presentation.FailuresShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows authentication failure and rethrows when auth fails")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAuthenticationFailsShowsFailureAndRethrows()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            ThrowOnAuth = true
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings()),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        Func<Task> act = () => app.RunAsync();

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        presentation.AuthenticationFailedShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows no issues matched filter when issue type filter excludes all loaded issues")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueTypeFilterExcludesAllIssuesShowsNoIssuesMatchedFilter()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings([new IssueTypeName("Bug"), new IssueTypeName("Story")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.DoneIssuesTableShown.Should().BeFalse();
    }

    [Fact(DisplayName = "RunAsync shows bug ratio section when bug issue names are configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenBugIssueNamesAreConfiguredShowsBugRatio()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Bug")),
            CreatedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open bug"))],
            MovedToDoneThisMonthIssues = [new IssueListItem(new IssueKey("AAA-1"), new IssueSummary("Done bug"))],
            RejectedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Rejected bug"))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.CreatedThisMonthIssuesRequested.Should().BeTrue();
        apiClient.MovedToDoneThisMonthIssuesRequested.Should().BeTrue();
        apiClient.RejectedThisMonthIssuesRequested.Should().BeTrue();
        presentation.AllTasksRatioLoadingStartedShown.Should().BeTrue();
        presentation.AllTasksRatioLoadingCompletedShown.Should().BeTrue();
        presentation.AllTasksRatioShown.Should().BeTrue();
        presentation.BugRatioLoadingStartedShown.Should().BeTrue();
        presentation.BugRatioLoadingCompletedShown.Should().BeTrue();
        presentation.BugRatioShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows all tasks ratio section without bug ratio details configuration")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncAlwaysShowsAllTasksRatio()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            CreatedThisMonthIssues =
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open task")),
                new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))
            ],
            MovedToDoneThisMonthIssues = [new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))],
            RejectedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-12"), new IssueSummary("Rejected task"))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.AllTasksRatioLoadingStartedShown.Should().BeTrue();
        presentation.AllTasksRatioLoadingCompletedShown.Should().BeTrue();
        presentation.AllTasksRatioShown.Should().BeTrue();
        presentation.BugRatioShown.Should().BeFalse();
        presentation.LastReportData!.Ratios.AllTasks!.CreatedThisMonth.Should().Be(new ItemCount(2));
        presentation.LastReportData.Ratios.AllTasks.OpenThisMonth.Should().Be(new ItemCount(1));
        presentation.LastReportData.Ratios.AllTasks.MovedToDoneThisMonth.Should().Be(new ItemCount(1));
        presentation.LastReportData.Ratios.AllTasks.RejectedThisMonth.Should().Be(new ItemCount(1));
        presentation.LastReportData.Ratios.AllTasks.FinishedThisMonth.Should().Be(new ItemCount(2));
    }

    [Fact(DisplayName = "RunAsync reuses all-tasks searches for report context when created-after is not configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenCreatedAfterIsMissingReusesAllTasksSearches()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-11")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-11"), new IssueTypeName("Task")),
            CreatedThisMonthIssues =
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open task")),
                new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))
            ],
            MovedToDoneThisMonthIssues = [new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))],
            RejectedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-12"), new IssueSummary("Rejected task"))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.IssueKeysMovedToDoneThisMonthRequestCount.Should().Be(0);
        apiClient.MovedToDoneThisMonthIssuesRequestCount.Should().Be(1);
        apiClient.RejectedThisMonthIssuesRequestCount.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync keeps dedicated key search when created-after is configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenCreatedAfterIsConfiguredKeepsDedicatedKeySearch()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-11")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-11"), new IssueTypeName("Task")),
            CreatedThisMonthIssues =
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open task")),
                new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))
            ],
            MovedToDoneThisMonthIssues = [new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Done task"))],
            RejectedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-12"), new IssueSummary("Rejected task"))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                createdAfter: new CreatedAfterDate("2026-01-01"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.IssueKeysMovedToDoneThisMonthRequestCount.Should().Be(2);
        apiClient.MovedToDoneThisMonthIssuesRequestCount.Should().Be(1);
        apiClient.RejectedThisMonthIssuesRequestCount.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync shows release report section when release report is configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseReportIsConfiguredShowsReleaseReport()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "Processing",
                    "Change completion date"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.ReleaseIssuesRequested.Should().BeTrue();
        presentation.ReleaseReportLoadingStartedShown.Should().BeTrue();
        presentation.ReleaseReportShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows release report before bug ratio and header when both are enabled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseAndBugRatioAreConfiguredShowsReleaseFirst()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Bug")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))],
            CreatedThisMonthIssues = [],
            MovedToDoneThisMonthIssues = [],
            RejectedThisMonthIssues = []
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ORX",
                    "Change completion date"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        var releaseLoadingStartIndex = presentation.Calls.IndexOf("ReleaseReportLoadingStarted");
        var releaseIndex = presentation.Calls.IndexOf("ReleaseReport");
        var periodContextIndex = presentation.Calls.IndexOf("ReportPeriodContext");
        var bugRatioLoadingStartIndex = presentation.Calls.IndexOf("BugRatioLoadingStarted");
        var bugRatioIndex = presentation.Calls.IndexOf("BugRatio");
        var headerIndex = presentation.Calls.IndexOf("ReportHeader");
        periodContextIndex.Should().BeGreaterThanOrEqualTo(0);
        releaseLoadingStartIndex.Should().BeGreaterThanOrEqualTo(0);
        releaseIndex.Should().BeGreaterThanOrEqualTo(0);
        bugRatioLoadingStartIndex.Should().BeGreaterThanOrEqualTo(0);
        bugRatioIndex.Should().BeGreaterThanOrEqualTo(0);
        headerIndex.Should().BeGreaterThanOrEqualTo(0);
        periodContextIndex.Should().BeLessThan(releaseLoadingStartIndex);
        releaseLoadingStartIndex.Should().BeLessThan(releaseIndex);
        bugRatioLoadingStartIndex.Should().BeLessThan(bugRatioIndex);
        releaseIndex.Should().BeLessThan(bugRatioIndex);
        releaseIndex.Should().BeLessThan(headerIndex);
    }

    [Fact(DisplayName = "RunAsync shows architecture tasks report immediately after release report")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseAndArchTasksAreConfiguredShowsArchTasksAfterRelease()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))],
            ArchTasks =
            [
                new ArchTaskItem(
                    new IssueKey("ADF-7"),
                    new IssueSummary("Architecture review"),
                    new DateTimeOffset(2026, 2, 2, 9, 30, 0, TimeSpan.Zero))
            ]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ADF",
                    "Change completion date"),
                archTasksReport: new ArchTasksReportSettings(
                    "project = ADF AND type = \"Arch Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.ReleaseIssuesRequested.Should().BeTrue();
        apiClient.ArchTasksRequested.Should().BeTrue();
        presentation.ReleaseReportShown.Should().BeTrue();
        presentation.ArchTasksReportShown.Should().BeTrue();

        var releaseIndex = presentation.Calls.IndexOf("ReleaseReport");
        var archTasksIndex = presentation.Calls.IndexOf("ArchTasksReport");
        var globalIncidentsIndex = presentation.Calls.IndexOf("GlobalIncidentsReport");
        releaseIndex.Should().BeGreaterThanOrEqualTo(0);
        archTasksIndex.Should().BeGreaterThanOrEqualTo(0);
        archTasksIndex.Should().BeGreaterThan(releaseIndex);
        globalIncidentsIndex.Should().Be(-1);
    }

    [Fact(DisplayName = "RunAsync shows global incidents report after release report")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseAndGlobalIncidentsAreConfiguredShowsGlobalIncidentsAfterRelease()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))],
            GlobalIncidents =
            [
                new GlobalIncidentItem(
                    new IssueKey("INC-1"),
                    new IssueSummary("ORX disabled"),
                    new DateTimeOffset(2026, 2, 12, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 2, 12, 10, 49, 0, TimeSpan.Zero))
            ]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ORX",
                    "Change completion date"),
                globalIncidentsReport: new GlobalIncidentsReportSettings(jqlFilter: "labels = SERVICE"))),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.ReleaseIssuesRequested.Should().BeTrue();
        apiClient.GlobalIncidentsRequested.Should().BeTrue();
        presentation.ReleaseReportShown.Should().BeTrue();
        presentation.GlobalIncidentsReportShown.Should().BeTrue();
        var releaseIndex = presentation.Calls.IndexOf("ReleaseReport");
        var globalIncidentsIndex = presentation.Calls.IndexOf("GlobalIncidentsReport");
        releaseIndex.Should().BeGreaterThanOrEqualTo(0);
        globalIncidentsIndex.Should().BeGreaterThan(releaseIndex);
        presentation.LastReportData.Should().NotBeNull();
        presentation.LastReportData!.Source.GlobalIncidents.Should().ContainSingle();
    }

    [Fact(DisplayName = "RunAsync shows rejected issues table when reject status is configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenRejectStatusIsConfiguredShowsRejectedIssuesTable()
    {
        // Arrange
        var rejectTransitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Reject"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var rejectedIssue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Rejected task"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            rejectTransitions,
            PathKey.FromTransitions(rejectTransitions),
            PathLabel.FromTransitions(rejectTransitions),
            hasPullRequest: true);

        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = rejectedIssue
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.RejectedIssuesTableShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync renders transition report when only rejected issues are found")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenOnlyRejectedIssuesAreFoundRendersTransitionReport()
    {
        // Arrange
        var rejectedTransitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Reject"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var rejectedIssue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Rejected task"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            rejectedTransitions,
            PathKey.FromTransitions(rejectedTransitions),
            PathLabel.FromTransitions(rejectedTransitions),
            hasPullRequest: true);
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [],
            RejectIssueKeys = [new IssueKey("AAA-1")],
            IssuesByKey =
            {
                ["AAA-1"] = rejectedIssue
            }
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        var exitCode = await app.RunAsync();

        // Assert
        exitCode.Should().Be(JiraApplicationExitCode.Success);
        presentation.NoIssuesMatchedFilterShown.Should().BeFalse();
        presentation.RejectedIssuesTableShown.Should().BeTrue();
        presentation.RejectedIssues.Should().Equal(rejectedIssue);
        presentation.ReportRendered.Should().BeTrue();
        presentation.LastReportData.Should().NotBeNull();
        presentation.LastReportData!.Source.SearchIssueCount.Should().Be(new ItemCount(1));
        presentation.LastReportData.Transitions.RejectedIssues.Should().Equal(rejectedIssue);
        presentation.LastReportData.Transitions.PathSummary.SuccessfulCount.Should().Be(new ItemCount(1));
    }

    [Fact(DisplayName = "RunAsync transition analysis uses only issues with code")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueHasNoCodeExcludesItFromTransitionAnalysis()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };

        var issueWithCode = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("With code"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: true);

        var issueWithoutCode = new IssueTimeline(
            new IssueKey("AAA-2"),
            new IssueTypeName("Task"),
            new IssueSummary("Without code"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: false);

        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            IssuesByKey =
            {
                ["AAA-1"] = issueWithCode,
                ["AAA-2"] = issueWithoutCode
            }
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.DoneIssuesTableShown.Should().BeTrue();
        presentation.DoneIssues.Should().HaveCount(2);
        presentation.DoneIssues.Select(static issue => issue.Key.Value).Should().BeEquivalentTo(["AAA-1", "AAA-2"]);
        presentation.PathGroupsSummary.Should().NotBeNull();
        presentation.PathGroupsSummary!.SuccessfulCount.Value.Should().Be(2);
        presentation.PathGroupsSummary.MatchedStageCount.Value.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync counts unique done and rejected issues in loading progress and shows processing steps")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenRejectedIssuesRequireSeparateLoadUpdatesProgressAndProcessingSteps()
    {
        // Arrange
        var doneIssue = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"));
        var rejectedTransitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("To Do"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("To Do"), new StatusName("Reject"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var rejectedIssue = new IssueTimeline(
            new IssueKey("AAA-2"),
            new IssueTypeName("Task"),
            new IssueSummary("Rejected task"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            rejectedTransitions,
            PathKey.FromTransitions(rejectedTransitions),
            PathLabel.FromTransitions(rejectedTransitions),
            hasPullRequest: true);

        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            RejectIssueKeys = [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            IssuesByKey =
            {
                ["AAA-1"] = doneIssue,
                ["AAA-2"] = rejectedIssue
            }
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.IssueLoadingStartedTotal.Should().Be(new ItemCount(2));
        presentation.IssueLoadingCompletedLoaded.Should().Be(new ItemCount(2));
        presentation.ProcessingSteps.Should().ContainInOrder(
            "Applying issue type and required-stage filters...",
            "Calculating transition metrics and percentiles...",
            "Building path groups...",
            "Rendering reports...");
    }

    [Fact(DisplayName = "RunAsync loads issue timelines with bulk API call")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenMultipleIssueTimelinesAreLoadedUsesBulkTimelineFetch()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys =
            [
                new IssueKey("AAA-1"),
                new IssueKey("AAA-2"),
                new IssueKey("AAA-3"),
                new IssueKey("AAA-4")
            ]
        };
        apiClient.IssuesByKey["AAA-1"] = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"));
        apiClient.IssuesByKey["AAA-2"] = CreateIssue(new IssueKey("AAA-2"), new IssueTypeName("Task"));
        apiClient.IssuesByKey["AAA-3"] = CreateIssue(new IssueKey("AAA-3"), new IssueTypeName("Task"));
        apiClient.IssuesByKey["AAA-4"] = CreateIssue(new IssueKey("AAA-4"), new IssueTypeName("Task"));

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.IssueTimelinesRequestCount.Should().Be(1);
        apiClient.SingleIssueTimelineRequestCount.Should().Be(0);
        presentation.IssueLoadingCompletedLoaded.Should().Be(new ItemCount(4));
    }

    [Fact(DisplayName = "RunAsync renders PDF report after transition analysis")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAnalysisCompletesRendersPdfReport()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.ReportRendered.Should().BeTrue();
        presentation.LastReportData.Should().NotBeNull();
        presentation.LastReportData!.Transitions.DoneIssues.Should().ContainSingle();
        presentation.LastReportData.Source.SearchIssueCount.Value.Should().Be(1);
    }

    [Fact(DisplayName = "RunAsync shows open issues by status summary after path groups")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAnalysisCompletesShowsOpenIssuesSummaryAtEnd()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            OpenIssuesByStatus =
            [
                new StatusIssueTypeSummary(
                    new StatusName("QA"),
                    new ItemCount(3),
                    [
                        new IssueTypeCountSummary(new IssueTypeName("UserStory"), new ItemCount(2)),
                        new IssueTypeCountSummary(new IssueTypeName("SubTask"), new ItemCount(1))
                    ])
            ]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.OpenIssuesByStatusRequested.Should().BeTrue();
        presentation.OpenIssuesByStatusShown.Should().BeTrue();
        var pathGroupsIndex = presentation.Calls.IndexOf("PathGroups");
        var openIssuesSummaryIndex = presentation.Calls.IndexOf("OpenIssuesByStatusSummary");
        pathGroupsIndex.Should().BeGreaterThanOrEqualTo(0);
        openIssuesSummaryIndex.Should().BeGreaterThan(pathGroupsIndex);
    }

    [Fact(DisplayName = "RunAsync shows done days-at-work 75P report after done issues table")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAnalysisCompletesShowsDoneDaysAtWork75PerTypeAfterDoneTable()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        presentation.DoneDaysAtWork75PerTypeShown.Should().BeTrue();
        var doneTableIndex = presentation.Calls.IndexOf("DoneIssuesTable");
        var p75ByTypeIndex = presentation.Calls.IndexOf("DoneDaysAtWork75PerType");
        doneTableIndex.Should().BeGreaterThanOrEqualTo(0);
        p75ByTypeIndex.Should().BeGreaterThan(doneTableIndex);
    }

    [Fact(DisplayName = "RunAsync does not load or show open issues summary when general statistics are disabled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenGeneralStatisticsAreDisabledSkipsOpenIssuesSummary()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            OpenIssuesByStatus =
            [
                new StatusIssueTypeSummary(
                    new StatusName("QA"),
                    new ItemCount(2),
                    [new IssueTypeCountSummary(new IssueTypeName("UserStory"), new ItemCount(2))])
            ]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService();
        var app = CreateApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                showGeneralStatistics: false)),
            CreateDataFacade(apiClient, presentation),
            CreateAnalysisFacade(logic),
            presentation,
            new FakeRequestTelemetryCollector());

        // Act
        await app.RunAsync();

        // Assert
        apiClient.OpenIssuesByStatusRequested.Should().BeFalse();
        presentation.OpenIssuesByStatusShown.Should().BeFalse();
        presentation.Calls.Should().NotContain("OpenIssuesByStatusSummary");
        presentation.LastReportData.Should().NotBeNull();
        presentation.LastReportData!.Settings.ShowGeneralStatistics.Should().BeFalse();
        presentation.LastReportData.Source.OpenIssuesByStatus.Should().BeEmpty();
    }

    private static JiraApplicationDataFacade CreateDataFacade(
        FakeApiClient apiClient,
        IJiraPresentationService presentationService)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(presentationService);

        return new JiraApplicationDataFacade(
            apiClient,
            new IssueSearchSnapshotLoader(apiClient),
            new JiraReportContextLoader(apiClient, apiClient),
            new JiraIssueTimelineLoader(apiClient, presentationService),
            new TestCoverageLoader(apiClient));
    }

    private static JiraApplicationAnalysisFacade CreateAnalysisFacade(IJiraLogicService logicService)
    {
        ArgumentNullException.ThrowIfNull(logicService);

        return new JiraApplicationAnalysisFacade(logicService);
    }

    private static JiraApplication CreateApplication(
        IOptions<AppSettings> settings,
        JiraApplicationDataFacade dataFacade,
        JiraApplicationAnalysisFacade analysisFacade,
        FakePresentationService presentation,
        FakeRequestTelemetryCollector requestTelemetryCollector)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(analysisFacade);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(requestTelemetryCollector);

        var appSettings = settings.Value;
        return new JiraApplication(
            presentation,
            requestTelemetryCollector,
            new JiraApplicationReportLoader(appSettings, dataFacade),
            new JiraApplicationReportPresenter(appSettings, presentation, presentation),
            new JiraApplicationAnalysisRunner(
                appSettings,
                dataFacade,
                analysisFacade,
                presentation,
                presentation,
                presentation,
                presentation,
                new ReportRunContext(
                    new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2)))));
    }

    private static AppSettings CreateSettings(
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        IReadOnlyList<IssueTypeName>? bugIssueNames = null,
        ReleaseReportSettings? releaseReport = null,
        ArchTasksReportSettings? archTasksReport = null,
        GlobalIncidentsReportSettings? globalIncidentsReport = null,
        bool showGeneralStatistics = true,
        CreatedAfterDate? createdAfter = null)
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            createdAfter,
            issueTypes,
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: false,
            bugIssueNames: bugIssueNames,
            showGeneralStatistics: showGeneralStatistics,
            releaseReport: releaseReport,
            archTasksReport: archTasksReport,
            globalIncidentsReport: globalIncidentsReport);
    }

    private static IssueTimeline CreateIssue(IssueKey key, IssueTypeName? issueType = null)
    {
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };

        return new IssueTimeline(
            key,
            issueType ?? new IssueTypeName("Story"),
            new IssueSummary($"Summary {key.Value}"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: true);
    }

}
