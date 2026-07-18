using System.Globalization;

using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationReportingFacadeTests
{
    [Fact(DisplayName = "Constructor throws when presentation service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPresentationServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraReportPipeline reportPipeline = new Mock<IJiraReportPipeline>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new JiraApplicationReportingFacade(null!, reportPipeline);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when report pipeline is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenReportPipelineIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraPresentationService presentationService = new Mock<IJiraPresentationService>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new JiraApplicationReportingFacade(presentationService, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Facade delegates all reporting calls to presentation service and report pipeline")]
    [Trait("Category", "Unit")]
    public void ReportingCallsWhenInvokedDelegateToUnderlyingServices()
    {
        // Arrange
        var presentationService = new Mock<IJiraPresentationService>(MockBehavior.Strict);
        var reportPipeline = new Mock<IJiraReportPipeline>(MockBehavior.Strict);
        var facade = new JiraApplicationReportingFacade(
            presentationService.Object,
            reportPipeline.Object);

        var user = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123");
        var errorMessage = new ErrorMessage("Search failed.");
        var reportPeriod = ReportPeriod.FromMonthLabel(new MonthLabel("2026-03"));
        var createdAfter = new CreatedAfterDate("2026-03-01");
        var settings = CreateSettings();
        var issueCount = new ItemCount(7);
        var telemetrySummary = new JiraRequestTelemetrySummary(3, 1, 1024, TimeSpan.FromSeconds(2), []);
        var releaseSettings = new ReleaseReportSettings(
            new ProjectKey("REL"),
            "Release Project",
            "Release date");
        var archTasksSettings = new ArchTasksReportSettings("project = ARCH");
        var globalIncidentsSettings = new GlobalIncidentsReportSettings(jqlFilter: "labels = INCIDENT");
        var releaseIssues = new[]
        {
            new ReleaseIssueItem(
                new IssueKey("REL-1"),
                new IssueSummary("Release"),
                new DateOnly(2026, 3, 10))
        };
        var archTasks = new[]
        {
            new ArchTaskItem(
                new IssueKey("ARCH-1"),
                new IssueSummary("Architecture task"),
                ParseUtc("2026-03-08T08:00:00+00:00"))
        };
        var globalIncidents = new[]
        {
            new GlobalIncidentItem(
                new IssueKey("INC-1"),
                new IssueSummary("Incident"),
                ParseUtc("2026-03-10T08:00:00+00:00"),
                ParseUtc("2026-03-10T10:00:00+00:00"),
                "High",
                "Major")
        };
        var issueRatioSnapshot = new IssueRatioSnapshot(
            new ItemCount(4),
            new ItemCount(1),
            new ItemCount(2),
            new ItemCount(1),
            new ItemCount(3),
            [],
            [],
            []);
        var bugIssueNames = new[] { new IssueTypeName("Bug") };
        var doneIssues = new[] { CreateIssue("APP-1") };
        var doneSummaries = new[]
        {
            new IssueTypeWorkDays75Summary(
                new IssueTypeName("Story"),
                new ItemCount(1),
                TimeSpan.FromDays(2))
        };
        var rejectedIssues = new[] { CreateIssue("APP-2") };
        var pathSummary = new PathGroupsSummary(
            new ItemCount(2),
            new ItemCount(2),
            new ItemCount(0),
            new ItemCount(1));
        var pathGroups = new[]
        {
            new PathGroup(
                new PathLabel("Open -> Done"),
                doneIssues,
                [],
                TimeSpan.FromDays(2))
        };
        var openIssuesByStatus = new[]
        {
            new StatusIssueTypeSummary(
                new StatusName("In Progress"),
                new ItemCount(2),
                [])
        };
        var failures = new[]
        {
            new LoadFailure(new IssueKey("APP-3"), new ErrorMessage("Timeout"))
        };
        var reportData = CreateReportData();

        presentationService.Setup(service => service.ShowAuthenticationStarted());
        presentationService.Setup(service => service.ShowAuthenticationSucceeded(user));
        presentationService.Setup(service => service.ShowAuthenticationFailed(errorMessage));
        presentationService.Setup(service => service.ShowReportPeriodContext(reportPeriod, createdAfter));
        presentationService.Setup(service => service.ShowIssueSearchFailed(errorMessage));
        presentationService.Setup(service => service.ShowReportHeader(settings, issueCount));
        presentationService.Setup(service => service.ShowNoIssuesMatchedFilter());
        presentationService.Setup(service => service.ShowProcessingStep("Loading issues..."));
        presentationService.As<IJiraStatusPresenter>().Setup(service => service.ShowSpacer());
        presentationService.Setup(service => service.ShowNoIssuesLoaded());
        presentationService.Setup(service => service.ShowNoIssuesMatchedRequiredStage());
        presentationService.Setup(service => service.ShowExecutionSummary(TimeSpan.FromSeconds(5), telemetrySummary));
        presentationService.Setup(service => service.ShowReleaseReportLoadingStarted());
        presentationService.Setup(service => service.ShowGlobalIncidentsReportLoadingStarted());
        presentationService.Setup(service => service.ShowArchTasksReportLoadingStarted());
        presentationService.Setup(service => service.ShowReleaseReport(releaseSettings, reportPeriod, releaseIssues));
        presentationService.Setup(service => service.ShowArchTasksReport(archTasksSettings, archTasks));
        presentationService.Setup(service => service.ShowGlobalIncidentsReport(
            globalIncidentsSettings,
            reportPeriod,
            globalIncidents));
        presentationService.Setup(service => service.ShowAllTasksRatioLoadingStarted());
        presentationService.Setup(service => service.ShowAllTasksRatioLoadingCompleted(issueRatioSnapshot));
        presentationService.Setup(service => service.ShowAllTasksRatio("Team", "A", issueRatioSnapshot));
        presentationService.Setup(service => service.ShowBugRatioLoadingStarted(bugIssueNames));
        presentationService.Setup(service => service.ShowBugRatioLoadingCompleted(issueRatioSnapshot));
        presentationService.Setup(service => service.ShowBugRatio(bugIssueNames, "Type", "Bug", issueRatioSnapshot));
        presentationService.Setup(service => service.ShowDoneIssuesTable(doneIssues, new StatusName("Done")));
        presentationService.Setup(service => service.ShowDoneDaysAtWork75PerType(doneSummaries, new StatusName("Done")));
        presentationService.Setup(service => service.ShowRejectedIssuesTable(rejectedIssues, new StatusName("Rejected")));
        presentationService.Setup(service => service.ShowPathGroupsSummary(pathSummary));
        presentationService.Setup(service => service.ShowPathGroups(pathGroups));
        presentationService.Setup(service => service.ShowOpenIssuesByStatusSummary(
            openIssuesByStatus,
            new StatusName("Done"),
            new StatusName("Rejected")));
        presentationService.Setup(service => service.ShowFailures(failures));
        reportPipeline.Setup(pipeline => pipeline.RenderReport(reportData));

        // Act
        facade.ShowAuthenticationStarted();
        facade.ShowAuthenticationSucceeded(user);
        facade.ShowAuthenticationFailed(errorMessage);
        facade.ShowReportPeriodContext(reportPeriod, createdAfter);
        facade.ShowIssueSearchFailed(errorMessage);
        facade.ShowReportHeader(settings, issueCount);
        facade.ShowNoIssuesMatchedFilter();
        facade.ShowProcessingStep("Loading issues...");
        facade.ShowSpacer();
        facade.ShowNoIssuesLoaded();
        facade.ShowNoIssuesMatchedRequiredStage();
        facade.ShowExecutionSummary(TimeSpan.FromSeconds(5), telemetrySummary);
        facade.ShowReleaseReportLoadingStarted();
        facade.ShowGlobalIncidentsReportLoadingStarted();
        facade.ShowArchTasksReportLoadingStarted();
        facade.ShowReleaseReport(releaseSettings, reportPeriod, releaseIssues);
        facade.ShowArchTasksReport(archTasksSettings, archTasks);
        facade.ShowGlobalIncidentsReport(globalIncidentsSettings, reportPeriod, globalIncidents);
        facade.ShowAllTasksRatioLoadingStarted();
        facade.ShowAllTasksRatioLoadingCompleted(issueRatioSnapshot);
        facade.ShowAllTasksRatio("Team", "A", issueRatioSnapshot);
        facade.ShowBugRatioLoadingStarted(bugIssueNames);
        facade.ShowBugRatioLoadingCompleted(issueRatioSnapshot);
        facade.ShowBugRatio(bugIssueNames, "Type", "Bug", issueRatioSnapshot);
        facade.ShowDoneIssuesTable(doneIssues, new StatusName("Done"));
        facade.ShowDoneDaysAtWork75PerType(doneSummaries, new StatusName("Done"));
        facade.ShowRejectedIssuesTable(rejectedIssues, new StatusName("Rejected"));
        facade.ShowPathGroupsSummary(pathSummary);
        facade.ShowPathGroups(pathGroups);
        facade.ShowOpenIssuesByStatusSummary(
            openIssuesByStatus,
            new StatusName("Done"),
            new StatusName("Rejected"));
        facade.ShowFailures(failures);
        facade.RenderReport(reportData);

        // Assert
        presentationService.Verify(service => service.ShowAuthenticationStarted(), Times.Once);
        presentationService.Verify(service => service.ShowAuthenticationSucceeded(user), Times.Once);
        presentationService.Verify(service => service.ShowAuthenticationFailed(errorMessage), Times.Once);
        presentationService.Verify(
            service => service.ShowReportPeriodContext(reportPeriod, createdAfter),
            Times.Once);
        presentationService.Verify(service => service.ShowIssueSearchFailed(errorMessage), Times.Once);
        presentationService.Verify(service => service.ShowReportHeader(settings, issueCount), Times.Once);
        presentationService.Verify(service => service.ShowNoIssuesMatchedFilter(), Times.Once);
        presentationService.Verify(service => service.ShowProcessingStep("Loading issues..."), Times.Once);
        presentationService.As<IJiraStatusPresenter>()
            .Verify(service => service.ShowSpacer(), Times.Once);
        presentationService.Verify(service => service.ShowNoIssuesLoaded(), Times.Once);
        presentationService.Verify(service => service.ShowNoIssuesMatchedRequiredStage(), Times.Once);
        presentationService.Verify(
            service => service.ShowExecutionSummary(TimeSpan.FromSeconds(5), telemetrySummary),
            Times.Once);
        presentationService.Verify(service => service.ShowReleaseReportLoadingStarted(), Times.Once);
        presentationService.Verify(service => service.ShowGlobalIncidentsReportLoadingStarted(), Times.Once);
        presentationService.Verify(service => service.ShowArchTasksReportLoadingStarted(), Times.Once);
        presentationService.Verify(
            service => service.ShowReleaseReport(releaseSettings, reportPeriod, releaseIssues),
            Times.Once);
        presentationService.Verify(
            service => service.ShowArchTasksReport(archTasksSettings, archTasks),
            Times.Once);
        presentationService.Verify(
            service => service.ShowGlobalIncidentsReport(
                globalIncidentsSettings,
                reportPeriod,
                globalIncidents),
            Times.Once);
        presentationService.Verify(service => service.ShowAllTasksRatioLoadingStarted(), Times.Once);
        presentationService.Verify(
            service => service.ShowAllTasksRatioLoadingCompleted(issueRatioSnapshot),
            Times.Once);
        presentationService.Verify(
            service => service.ShowAllTasksRatio("Team", "A", issueRatioSnapshot),
            Times.Once);
        presentationService.Verify(
            service => service.ShowBugRatioLoadingStarted(bugIssueNames),
            Times.Once);
        presentationService.Verify(
            service => service.ShowBugRatioLoadingCompleted(issueRatioSnapshot),
            Times.Once);
        presentationService.Verify(
            service => service.ShowBugRatio(bugIssueNames, "Type", "Bug", issueRatioSnapshot),
            Times.Once);
        presentationService.Verify(
            service => service.ShowDoneIssuesTable(doneIssues, new StatusName("Done")),
            Times.Once);
        presentationService.Verify(
            service => service.ShowDoneDaysAtWork75PerType(doneSummaries, new StatusName("Done")),
            Times.Once);
        presentationService.Verify(
            service => service.ShowRejectedIssuesTable(rejectedIssues, new StatusName("Rejected")),
            Times.Once);
        presentationService.Verify(service => service.ShowPathGroupsSummary(pathSummary), Times.Once);
        presentationService.Verify(service => service.ShowPathGroups(pathGroups), Times.Once);
        presentationService.Verify(
            service => service.ShowOpenIssuesByStatusSummary(
                openIssuesByStatus,
                new StatusName("Done"),
                new StatusName("Rejected")),
            Times.Once);
        presentationService.Verify(service => service.ShowFailures(failures), Times.Once);
        reportPipeline.Verify(pipeline => pipeline.RenderReport(reportData), Times.Once);
    }

    private static AppSettings CreateSettings()
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token-value"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            new StatusName("Rejected"),
            [new StageName("In Progress")],
            ReportPeriod.FromMonthLabel(new MonthLabel("2026-03")));
    }

    private static JiraReportData CreateReportData()
    {
        return new JiraReportData
        {
            Settings = CreateSettings(),
            Source = new JiraReportSourceData { SearchIssueCount = new ItemCount(1) },
            Transitions = new JiraReportTransitionData
            {
                PathSummary = new PathGroupsSummary(
                    new ItemCount(1),
                    new ItemCount(1),
                    new ItemCount(0),
                    new ItemCount(1))
            }
        };
    }

    private static IssueTimeline CreateIssue(string issueKey)
    {
        var transitions = new[]
        {
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("Done"),
                    ParseUtc("2026-03-10T09:00:00+00:00"),
                    TimeSpan.FromHours(3))
        };

        return new IssueTimeline(
            new IssueKey(issueKey),
            new IssueTypeName("Story"),
            new IssueSummary($"Summary for {issueKey}"),
            ParseUtc("2026-03-09T09:00:00+00:00"),
            ParseUtc("2026-03-10T09:00:00+00:00"),
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
}
