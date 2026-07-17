using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationBranchesTests
{
    [Fact(DisplayName = "RunAsync handles report-context HTTP failures as issue-search failures")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReportContextLoadThrowsHttpRequestExceptionShowsIssueSearchFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ThrowsAsync(new HttpRequestException("Network failure."));
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(issueTypes => issueTypes.Count == 0),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateRatioSnapshot());
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        await app.RunAsync(cts.Token);

        // Assert
        reportingFacade.Verify(
            facade => facade.ShowIssueSearchFailed(It.Is<ErrorMessage>(message => message.Value.Contains("Network failure.", StringComparison.Ordinal))),
            Times.Once);
        reportingFacade.Verify(facade => facade.ShowReportHeader(It.IsAny<AppSettings>(), It.IsAny<ItemCount>()), Times.Never);
        reportingFacade.Verify(facade => facade.RenderReport(It.IsAny<JiraReportData>()), Times.Never);
        reportingFacade.Verify(facade => facade.ShowExecutionSummary(It.IsAny<TimeSpan>(), It.IsAny<JiraRequestTelemetrySummary>()), Times.Once);
        analysisFacade.Verify(
            facade => facade.Analyze(
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<LoadFailure>>(),
                It.IsAny<AppSettings>()),
            Times.Never);
    }

    [Fact(DisplayName = "RunAsync stops before analysis when all-tasks ratio loading throws invalid operation")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAllTasksRatioLoadThrowsInvalidOperationExceptionShowsIssueSearchFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateReportContext());
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(issueTypes => issueTypes.Count == 0),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ThrowsAsync(new InvalidOperationException("Ratio data is unavailable."));
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        await app.RunAsync(cts.Token);

        // Assert
        reportingFacade.Verify(facade => facade.ShowAllTasksRatioLoadingStarted(), Times.Once);
        reportingFacade.Verify(
            facade => facade.ShowIssueSearchFailed(It.Is<ErrorMessage>(message => message.Value.Contains("Ratio data is unavailable.", StringComparison.Ordinal))),
            Times.Once);
        reportingFacade.Verify(facade => facade.ShowAllTasksRatioLoadingCompleted(It.IsAny<IssueRatioSnapshot>()), Times.Never);
        reportingFacade.Verify(facade => facade.ShowReportHeader(It.IsAny<AppSettings>(), It.IsAny<ItemCount>()), Times.Never);
        dataFacade.Verify(
            facade => facade.LoadIssueTimelinesAsync(
                It.IsAny<IReadOnlyList<IssueKey>>(),
                It.IsAny<IReadOnlyList<IssueKey>>(),
                cts.Token),
            Times.Never);
    }

    [Fact(DisplayName = "RunAsync stops before analysis when bug-ratio loading throws JSON exception")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenBugRatioLoadThrowsJsonExceptionShowsIssueSearchFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings(bugIssueNames: [new IssueTypeName("Bug")]);
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateReportContext());
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(issueTypes => issueTypes.Count == 0),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateRatioSnapshot());
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(issueTypes => issueTypes.Count == 1 && issueTypes[0].Value == "Bug"),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ThrowsAsync(new JsonException("Bug ratio payload is invalid."));
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        await app.RunAsync(cts.Token);

        // Assert
        reportingFacade.Verify(facade => facade.ShowBugRatioLoadingStarted(settings.BugIssueNames), Times.Once);
        reportingFacade.Verify(facade => facade.ShowAllTasksRatioLoadingCompleted(It.IsAny<IssueRatioSnapshot>()), Times.Once);
        reportingFacade.Verify(facade => facade.ShowAllTasksRatio(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<IssueRatioSnapshot>()), Times.Once);
        reportingFacade.Verify(
            facade => facade.ShowIssueSearchFailed(It.Is<ErrorMessage>(message => message.Value.Contains("Bug ratio payload is invalid.", StringComparison.Ordinal))),
            Times.Once);
        reportingFacade.Verify(facade => facade.ShowBugRatioLoadingCompleted(It.IsAny<IssueRatioSnapshot>()), Times.Never);
        reportingFacade.Verify(facade => facade.ShowReportHeader(It.IsAny<AppSettings>(), It.IsAny<ItemCount>()), Times.Never);
    }

    [Fact(DisplayName = "RunAsync shows no issues loaded when timeline loading returns only failures")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenNoIssuesLoadSuccessfullyShowsNoIssuesLoaded()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());
        var failures = new[]
        {
            new LoadFailure(new IssueKey("APP-2"), new ErrorMessage("Failed to load issue."))
        };
        var reportContext = CreateReportContext();

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(reportContext);
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.IsAny<IReadOnlyList<IssueTypeName>>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateRatioSnapshot());
        dataFacade.Setup(facade => facade.LoadIssueTimelinesAsync(
                reportContext.IssueKeys,
                reportContext.RejectIssueKeys,
                cts.Token))
            .ReturnsAsync(new IssueTimelineLoadResult([], [], failures, new ItemCount(0)));
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        await app.RunAsync(cts.Token);

        // Assert
        reportingFacade.Verify(facade => facade.ShowNoIssuesLoaded(), Times.Once);
        reportingFacade.Verify(facade => facade.ShowFailures(failures), Times.Once);
        reportingFacade.Verify(
            facade => facade.ShowOpenIssuesByStatusSummary(
                reportContext.OpenIssuesByStatus,
                settings.DoneStatusName,
                settings.RejectStatusName),
            Times.Once);
        reportingFacade.Verify(
            facade => facade.RenderReport(It.Is<JiraReportData>(report =>
                report.Source.SearchIssueCount == new ItemCount(1)
                && report.Transitions.DoneIssues.Count == 0
                && report.Transitions.PathSummary.SuccessfulCount == new ItemCount(0)
                && report.Transitions.PathSummary.PathGroupCount == new ItemCount(0)
                && report.Failures.Count == 1)),
            Times.Once);
        analysisFacade.Verify(
            facade => facade.Analyze(
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<LoadFailure>>(),
                It.IsAny<AppSettings>()),
            Times.Never);
    }

    [Fact(DisplayName = "RunAsync shows no required-stage match when analysis returns that outcome")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAnalysisFindsNoRequiredStageMatchShowsExpectedMessage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());
        var failures = new[]
        {
            new LoadFailure(new IssueKey("APP-3"), new ErrorMessage("Partial load failure."))
        };
        var reportContext = CreateReportContext();
        var loadedIssue = CreateIssue("APP-1");

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(reportContext);
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.IsAny<IReadOnlyList<IssueTypeName>>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateRatioSnapshot());
        dataFacade.Setup(facade => facade.LoadIssueTimelinesAsync(
                reportContext.IssueKeys,
                reportContext.RejectIssueKeys,
                cts.Token))
            .ReturnsAsync(new IssueTimelineLoadResult([loadedIssue], [], failures, new ItemCount(1)));
        analysisFacade.Setup(facade => facade.Analyze(
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                failures,
                settings))
            .Returns(JiraIssueAnalysisResult.NoIssuesMatchedRequiredStage());
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        await app.RunAsync(cts.Token);

        // Assert
        reportingFacade.Verify(facade => facade.ShowNoIssuesMatchedRequiredStage(), Times.Once);
        reportingFacade.Verify(facade => facade.ShowFailures(failures), Times.Once);
        reportingFacade.Verify(
            facade => facade.RenderReport(It.Is<JiraReportData>(report =>
                report.Source.SearchIssueCount == new ItemCount(1)
                && report.Transitions.DoneIssues.Count == 0
                && report.Transitions.PathSummary.SuccessfulCount == new ItemCount(1)
                && report.Transitions.PathSummary.MatchedStageCount == new ItemCount(0)
                && report.Transitions.PathSummary.PathGroupCount == new ItemCount(0)
                && report.Failures.Count == 1)),
            Times.Once);
    }

    [Fact(DisplayName = "RunAsync rethrows when analysis outcome is unsupported")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAnalysisOutcomeIsUnsupportedThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        var analysisFacade = new Mock<IJiraApplicationAnalysisFacade>(MockBehavior.Strict);
        var reportingFacade = CreateReportingFacade();
        var telemetryCollector = new Mock<IJiraRequestTelemetryCollector>(MockBehavior.Strict);
        telemetryCollector.Setup(collector => collector.Reset());
        var reportContext = CreateReportContext();
        var loadedIssue = CreateIssue("APP-1");

        dataFacade.Setup(facade => facade.GetCurrentUserAsync(cts.Token))
            .ReturnsAsync(CreateUser());
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(reportContext);
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.IsAny<IReadOnlyList<IssueTypeName>>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .ReturnsAsync(CreateRatioSnapshot());
        dataFacade.Setup(facade => facade.LoadIssueTimelinesAsync(
                reportContext.IssueKeys,
                reportContext.RejectIssueKeys,
                cts.Token))
            .ReturnsAsync(new IssueTimelineLoadResult([loadedIssue], [], [], new ItemCount(1)));
        analysisFacade.Setup(facade => facade.Analyze(
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<IssueTimeline>>(),
                It.IsAny<IReadOnlyList<LoadFailure>>(),
                settings))
            .Returns(new JiraIssueAnalysisResult
            {
                Outcome = (JiraIssueAnalysisOutcome)999
            });
        telemetryCollector.Setup(collector => collector.GetSummary())
            .Returns(new JiraRequestTelemetrySummary(0, 0, 0, TimeSpan.Zero, []));

        var app = CreateApplication(settings, dataFacade, analysisFacade, reportingFacade, telemetryCollector);

        // Act
        Func<Task> act = () => app.RunAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unsupported analysis outcome*");
        reportingFacade.Verify(facade => facade.ShowExecutionSummary(It.IsAny<TimeSpan>(), It.IsAny<JiraRequestTelemetrySummary>()), Times.Once);
    }

    private static Mock<IJiraApplicationReportingFacade> CreateReportingFacade()
    {
        var reportingFacade = new Mock<IJiraApplicationReportingFacade>(MockBehavior.Strict);
        reportingFacade.Setup(facade => facade.ShowAuthenticationStarted());
        reportingFacade.Setup(facade => facade.ShowAuthenticationSucceeded(It.IsAny<JiraAuthUser>()));
        reportingFacade.Setup(facade => facade.ShowAuthenticationFailed(It.IsAny<ErrorMessage>()));
        reportingFacade.Setup(facade => facade.ShowReportPeriodContext(
            It.IsAny<ReportPeriod>(),
            It.IsAny<CreatedAfterDate?>()));
        reportingFacade.Setup(facade => facade.ShowIssueSearchFailed(It.IsAny<ErrorMessage>()));
        reportingFacade.Setup(facade => facade.ShowReportHeader(It.IsAny<AppSettings>(), It.IsAny<ItemCount>()));
        reportingFacade.Setup(facade => facade.ShowNoIssuesMatchedFilter());
        reportingFacade.Setup(facade => facade.ShowProcessingStep(It.IsAny<string>()));
        reportingFacade.Setup(facade => facade.ShowSpacer());
        reportingFacade.Setup(facade => facade.ShowNoIssuesLoaded());
        reportingFacade.Setup(facade => facade.ShowNoIssuesMatchedRequiredStage());
        reportingFacade.Setup(facade => facade.ShowExecutionSummary(
            It.IsAny<TimeSpan>(),
            It.IsAny<JiraRequestTelemetrySummary>()));
        reportingFacade.Setup(facade => facade.ShowAllTasksRatioLoadingStarted());
        reportingFacade.Setup(facade => facade.ShowAllTasksRatioLoadingCompleted(It.IsAny<IssueRatioSnapshot>()));
        reportingFacade.Setup(facade => facade.ShowAllTasksRatio(
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IssueRatioSnapshot>()));
        reportingFacade.Setup(facade => facade.ShowBugRatioLoadingStarted(It.IsAny<IReadOnlyList<IssueTypeName>>()));
        reportingFacade.Setup(facade => facade.ShowBugRatioLoadingCompleted(It.IsAny<IssueRatioSnapshot>()));
        reportingFacade.Setup(facade => facade.ShowBugRatio(
            It.IsAny<IReadOnlyList<IssueTypeName>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IssueRatioSnapshot>()));
        reportingFacade.Setup(facade => facade.ShowOpenIssuesByStatusSummary(
            It.IsAny<IReadOnlyList<StatusIssueTypeSummary>>(),
            It.IsAny<StatusName>(),
            It.IsAny<StatusName?>()));
        reportingFacade.Setup(facade => facade.ShowFailures(It.IsAny<IReadOnlyList<LoadFailure>>()));
        reportingFacade.Setup(facade => facade.RenderReport(It.IsAny<JiraReportData>()));
        return reportingFacade;
    }

    private static JiraApplication CreateApplication(
        AppSettings settings,
        Mock<IJiraApplicationDataFacade> dataFacade,
        Mock<IJiraApplicationAnalysisFacade> analysisFacade,
        Mock<IJiraApplicationReportingFacade> reportingFacade,
        Mock<IJiraRequestTelemetryCollector> telemetryCollector)
    {
        return new JiraApplication(
            reportingFacade.Object,
            telemetryCollector.Object,
            new JiraApplicationReportLoader(settings, dataFacade.Object, reportingFacade.Object),
            new JiraApplicationAnalysisRunner(
                settings,
                dataFacade.Object,
                analysisFacade.Object,
                reportingFacade.Object));
    }

    private static AppSettings CreateSettings(IReadOnlyList<IssueTypeName>? bugIssueNames = null)
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token-value"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            new StatusName("Rejected"),
            [new StageName("Code Review")],
            ReportPeriod.FromMonthLabel(new MonthLabel("2026-03")),
            bugIssueNames: bugIssueNames);
    }

    private static JiraAuthUser CreateUser() =>
        new(new UserDisplayName("Nikita"), "user@example.com", "123");

    private static JiraReportContext CreateReportContext()
    {
        return new JiraReportContext(
            [new IssueKey("APP-1")],
            [],
            [],
            [],
            [],
            [],
            [new StatusIssueTypeSummary(new StatusName("In Progress"), new ItemCount(2), [])],
            []);
    }

    private static IssueRatioSnapshot CreateRatioSnapshot()
    {
        return new IssueRatioSnapshot(
            new ItemCount(3),
            new ItemCount(1),
            new ItemCount(1),
            new ItemCount(0),
            new ItemCount(1),
            [],
            [],
            []);
    }

    private static IssueTimeline CreateIssue(string key)
    {
        var transitions = new[]
        {
            new TransitionEvent(
                new StatusName("Open"),
                new StatusName("Done"),
                new DateTimeOffset(2026, 3, 10, 10, 0, 0, TimeSpan.Zero),
                TimeSpan.FromHours(5))
        };

        return new IssueTimeline(
            new IssueKey(key),
            new IssueTypeName("Story"),
            new IssueSummary($"Summary {key}"),
            new DateTimeOffset(2026, 3, 9, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 10, 10, 0, 0, TimeSpan.Zero),
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }
}
