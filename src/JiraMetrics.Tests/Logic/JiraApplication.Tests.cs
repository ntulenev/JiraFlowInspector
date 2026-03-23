using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationTests
{
    [Fact(DisplayName = "Constructor throws when settings are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSettingsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<AppSettings> settings = null!;
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();
        var pdfReportRenderer = new FakePdfReportRenderer();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when API client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        IJiraApiClient apiClient = null!;
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();
        var pdfReportRenderer = new FakePdfReportRenderer();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when settings value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSettingsValueIsNullThrowsArgumentException()
    {
        // Arrange
        var settings = Options.Create<AppSettings>(null!);
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();
        var pdfReportRenderer = new FakePdfReportRenderer();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when logic service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenLogicServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        var apiClient = new FakeApiClient();
        IJiraLogicService logic = null!;
        var presentation = new FakePresentationService();
        var pdfReportRenderer = new FakePdfReportRenderer();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when presentation service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPresentationServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        IJiraPresentationService presentation = null!;
        var pdfReportRenderer = new FakePdfReportRenderer();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when PDF report renderer is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPdfReportRendererIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();
        IPdfReportRenderer pdfReportRenderer = null!;

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation, pdfReportRenderer);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings()),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.DoneIssuesTableShown.Should().BeFalse();
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings()),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings()),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings([new IssueTypeName("Bug"), new IssueTypeName("Story")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        apiClient.CreatedThisMonthCountRequested.Should().BeFalse();
        apiClient.MovedToDoneThisMonthCountRequested.Should().BeFalse();
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        presentation.AllTasksRatioLoadingStartedShown.Should().BeTrue();
        presentation.AllTasksRatioLoadingCompletedShown.Should().BeTrue();
        presentation.AllTasksRatioShown.Should().BeTrue();
        presentation.BugRatioShown.Should().BeFalse();
        pdfReportRenderer.LastReportData!.AllTasksCreatedThisMonth.Should().Be(new ItemCount(2));
        pdfReportRenderer.LastReportData!.AllTasksOpenThisMonth.Should().Be(new ItemCount(1));
        pdfReportRenderer.LastReportData!.AllTasksMovedToDoneThisMonth.Should().Be(new ItemCount(1));
        pdfReportRenderer.LastReportData!.AllTasksRejectedThisMonth.Should().Be(new ItemCount(1));
        pdfReportRenderer.LastReportData!.AllTasksFinishedThisMonth.Should().Be(new ItemCount(2));
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "Processing",
                    "Change completion date"))),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ORX",
                    "Change completion date"))),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        releaseIndex.Should().BeLessThan(bugRatioLoadingStartIndex);
        releaseIndex.Should().BeLessThan(bugRatioIndex);
        releaseIndex.Should().BeLessThan(headerIndex);
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ORX",
                    "Change completion date"),
                globalIncidentsReport: new GlobalIncidentsReportSettings(jqlFilter: "labels = SERVICE"))),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        pdfReportRenderer.LastReportData.Should().NotBeNull();
        pdfReportRenderer.LastReportData!.GlobalIncidents.Should().ContainSingle();
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        presentation.RejectedIssuesTableShown.Should().BeTrue();
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        presentation.IssueLoadingStartedTotal.Should().Be(new ItemCount(2));
        presentation.IssueLoadingCompletedLoaded.Should().Be(new ItemCount(2));
        presentation.ProcessingSteps.Should().ContainInOrder(
            "Applying issue type and required-stage filters...",
            "Calculating transition metrics and percentiles...",
            "Building path groups...",
            "Rendering PDF report...");
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        pdfReportRenderer.ReportRendered.Should().BeTrue();
        pdfReportRenderer.LastReportData.Should().NotBeNull();
        pdfReportRenderer.LastReportData!.DoneIssues.Should().ContainSingle();
        pdfReportRenderer.LastReportData.SearchIssueCount.Value.Should().Be(1);
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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

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
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var pdfReportRenderer = new FakePdfReportRenderer();
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                showGeneralStatistics: false)),
            apiClient,
            logic,
            presentation,
            pdfReportRenderer);

        // Act
        await app.RunAsync();

        // Assert
        apiClient.OpenIssuesByStatusRequested.Should().BeFalse();
        presentation.OpenIssuesByStatusShown.Should().BeFalse();
        presentation.Calls.Should().NotContain("OpenIssuesByStatusSummary");
        pdfReportRenderer.LastReportData.Should().NotBeNull();
        pdfReportRenderer.LastReportData!.Settings.ShowGeneralStatistics.Should().BeFalse();
        pdfReportRenderer.LastReportData.OpenIssuesByStatus.Should().BeEmpty();
    }

    private static AppSettings CreateSettings(
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        IReadOnlyList<IssueTypeName>? bugIssueNames = null,
        ReleaseReportSettings? releaseReport = null,
        GlobalIncidentsReportSettings? globalIncidentsReport = null,
        bool showGeneralStatistics = true)
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
            null,
            issueTypes,
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: false,
            bugIssueNames: bugIssueNames,
            showGeneralStatistics: showGeneralStatistics,
            releaseReport: releaseReport,
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

    private sealed class FakeApiClient : IJiraApiClient
    {
        public JiraAuthUser CurrentUser { get; set; } = new(new UserDisplayName("unknown"), null, null);

        public IReadOnlyList<IssueKey> IssueKeys { get; set; } = [];

        public IReadOnlyList<IssueKey> RejectIssueKeys { get; set; } = [];

        public HashSet<IssueKey> FailIssueKeys { get; set; } = [];

        public IssueTimeline? IssueToReturn { get; set; }

        public Dictionary<string, IssueTimeline> IssuesByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool ThrowOnAuth { get; set; }

        public ItemCount CreatedThisMonthCount { get; set; } = new(0);

        public ItemCount MovedToDoneThisMonthCount { get; set; } = new(0);

        public IReadOnlyList<IssueListItem> CreatedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> MovedToDoneThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> RejectedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<StatusIssueTypeSummary> OpenIssuesByStatus { get; set; } = [];

        public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; set; } = [];

        public IReadOnlyList<GlobalIncidentItem> GlobalIncidents { get; set; } = [];

        public bool CreatedThisMonthCountRequested { get; private set; }

        public bool MovedToDoneThisMonthCountRequested { get; private set; }

        public bool CreatedThisMonthIssuesRequested { get; private set; }

        public bool MovedToDoneThisMonthIssuesRequested { get; private set; }

        public bool RejectedThisMonthIssuesRequested { get; private set; }

        public bool OpenIssuesByStatusRequested { get; private set; }

        public bool ReleaseIssuesRequested { get; private set; }

        public bool GlobalIncidentsRequested { get; private set; }

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
            return Task.FromResult(
                string.Equals(doneStatusName.Value, "Reject", StringComparison.OrdinalIgnoreCase)
                    ? (RejectIssueKeys.Count == 0 ? IssueKeys : RejectIssueKeys)
                    : IssueKeys);
        }

        public Task<ItemCount> GetIssueCountCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            CreatedThisMonthCountRequested = true;
            return Task.FromResult(CreatedThisMonthCount);
        }

        public Task<ItemCount> GetIssueCountMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            MovedToDoneThisMonthCountRequested = true;
            return Task.FromResult(MovedToDoneThisMonthCount);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            CreatedThisMonthIssuesRequested = true;
            return Task.FromResult(CreatedThisMonthIssues);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            if (string.Equals(doneStatusName.Value, "Reject", StringComparison.OrdinalIgnoreCase))
            {
                RejectedThisMonthIssuesRequested = true;
                return Task.FromResult(RejectedThisMonthIssues);
            }

            MovedToDoneThisMonthIssuesRequested = true;
            return Task.FromResult(MovedToDoneThisMonthIssues);
        }

        public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
            ProjectKey releaseProjectKey,
            string projectLabel,
            string releaseDateFieldName,
            string? componentsFieldName,
            IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
            string rollbackFieldName,
            string? environmentFieldName,
            string? environmentFieldValue,
            CancellationToken cancellationToken)
        {
            ReleaseIssuesRequested = true;
            return Task.FromResult(ReleaseIssues);
        }

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

        public Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken)
        {
            if (FailIssueKeys.Contains(issueKey))
            {
                throw new InvalidOperationException("Failed to load issue.");
            }

            if (IssuesByKey.TryGetValue(issueKey.Value, out var configuredIssue))
            {
                return Task.FromResult(new IssueTimeline(
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
            }

            if (IssueToReturn is null)
            {
                throw new InvalidOperationException("No issue configured for fake transport.");
            }

            return Task.FromResult(new IssueTimeline(
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

    private sealed class FakePdfReportRenderer : IPdfReportRenderer
    {
        public bool ReportRendered { get; private set; }

        public JiraPdfReportData? LastReportData { get; private set; }

        public void RenderReport(JiraPdfReportData reportData)
        {
            ReportRendered = true;
            LastReportData = reportData;
        }
    }

    private sealed class FakePresentationService : IJiraPresentationService
    {
        public List<string> Calls { get; } = [];

        public bool AuthenticationFailedShown { get; private set; }

        public bool NoIssuesMatchedFilterShown { get; private set; }

        public bool DoneIssuesTableShown { get; private set; }

        public bool RejectedIssuesTableShown { get; private set; }

        public bool FailuresShown { get; private set; }

        public bool BugRatioShown { get; private set; }

        public bool BugRatioLoadingStartedShown { get; private set; }

        public bool BugRatioLoadingCompletedShown { get; private set; }

        public bool AllTasksRatioShown { get; private set; }

        public bool AllTasksRatioLoadingStartedShown { get; private set; }

        public bool AllTasksRatioLoadingCompletedShown { get; private set; }

        public bool ReleaseReportShown { get; private set; }

        public bool ReleaseReportLoadingStartedShown { get; private set; }

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

        public void ShowReportPeriodContext(MonthLabel monthLabel, CreatedAfterDate? createdAfter) => Calls.Add("ReportPeriodContext");

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
            MonthLabel monthLabel,
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

        public void ShowGlobalIncidentsReportLoadingStarted()
        {
            GlobalIncidentsReportLoadingStartedShown = true;
            Calls.Add("GlobalIncidentsReportLoadingStarted");
        }

        public void ShowGlobalIncidentsReport(
            GlobalIncidentsReportSettings settings,
            MonthLabel monthLabel,
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

        public void ShowAllTasksRatioLoadingCompleted(
            ItemCount createdThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth)
        {
            AllTasksRatioLoadingCompletedShown = true;
            Calls.Add("AllTasksRatioLoadingCompleted");
        }

        public void ShowAllTasksRatio(
            string? customFieldName,
            string? customFieldValue,
            ItemCount createdThisMonth,
            ItemCount openThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth)
        {
            AllTasksRatioShown = true;
            Calls.Add("AllTasksRatio");
        }

        public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
        {
            BugRatioLoadingStartedShown = true;
            Calls.Add("BugRatioLoadingStarted");
        }

        public void ShowBugRatioLoadingCompleted(
            ItemCount createdThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth)
        {
            BugRatioLoadingCompletedShown = true;
            Calls.Add("BugRatioLoadingCompleted");
        }

        public void ShowBugRatio(
            IReadOnlyList<IssueTypeName> bugIssueNames,
            string? customFieldName,
            string? customFieldValue,
            ItemCount createdThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth,
            IReadOnlyList<IssueListItem> openIssues,
            IReadOnlyList<IssueListItem> doneIssues,
            IReadOnlyList<IssueListItem> rejectedIssues)
        {
            BugRatioShown = true;
            Calls.Add("BugRatio");
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
    }
}

