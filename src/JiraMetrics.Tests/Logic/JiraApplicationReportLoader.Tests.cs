using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationReportLoaderTests
{
    [Fact(DisplayName = "LoadAsync returns internal incidents and test coverage when optional loads succeed")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenOptionalLoadsSucceedReturnsTheirData()
    {
        // Arrange
        var coverageSettings = new TestCoverageSettings();
        var settings = CreateSettings(coverageSettings, [new IssueTypeName("Incident")]);
        var allTasksRatio = CreateRatioSnapshot(3);
        var internalIncidents = CreateRatioSnapshot(2);
        var testCoverage = new TestCoverageSnapshot([], []);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, allTasksRatio);
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 1 && types[0].Value == "Incident"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(internalIncidents);
        dataFacade.Setup(facade => facade.LoadTestCoverageAsync(
                settings,
                coverageSettings,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testCoverage);

        var sut = new JiraApplicationReportLoader(
            settings,
            dataFacade.Object);

        // Act
        var result = await sut.LoadAsync(cts.Token);

        // Assert
        var success = result.Should().BeOfType<ReportLoadResult.Success>().Subject;
        success.ReportData.InternalIncidents.Should().BeSameAs(internalIncidents);
        success.ReportData.TestCoverage.Should().BeSameAs(testCoverage);
    }

    [Fact(DisplayName = "LoadAsync returns failure when internal-incident loading fails")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenInternalIncidentLoadFailsReturnsFailure()
    {
        // Arrange
        var settings = CreateSettings(null, [new IssueTypeName("Incident")]);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3));
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 1),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Invalid incident response."));

        var sut = new JiraApplicationReportLoader(
            settings,
            dataFacade.Object);

        // Act
        var result = await sut.LoadAsync(cts.Token);

        // Assert
        var failure = result.Should().BeOfType<ReportLoadResult.Failure>().Subject;
        failure.Error.Value.Should().Contain("Invalid incident response.");
    }

    [Fact(DisplayName = "LoadAsync returns failure when test-coverage loading fails")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenTestCoverageLoadFailsReturnsFailure()
    {
        // Arrange
        var coverageSettings = new TestCoverageSettings();
        var settings = CreateSettings(coverageSettings, null);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3));
        dataFacade.Setup(facade => facade.LoadTestCoverageAsync(
                settings,
                coverageSettings,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Coverage service unavailable."));

        var sut = new JiraApplicationReportLoader(
            settings,
            dataFacade.Object);

        // Act
        var result = await sut.LoadAsync(cts.Token);

        // Assert
        var failure = result.Should().BeOfType<ReportLoadResult.Failure>().Subject;
        failure.Error.Value.Should().Contain("Coverage service unavailable.");
    }

    [Fact(DisplayName = "LoadAsync skips test-coverage loading when the feature is disabled")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenTestCoverageIsDisabledSkipsCoverageRequest()
    {
        // Arrange
        var settings = CreateSettings(new TestCoverageSettings(enabled: false), null);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3));
        var sut = new JiraApplicationReportLoader(
            settings,
            dataFacade.Object);

        // Act
        var result = await sut.LoadAsync(cts.Token);

        // Assert
        var success = result.Should().BeOfType<ReportLoadResult.Success>().Subject;
        success.ReportData.TestCoverage.Should().BeSameAs(TestCoverageSnapshot.Empty);
        dataFacade.Verify(
            facade => facade.LoadTestCoverageAsync(
                It.IsAny<AppSettings>(),
                It.IsAny<TestCoverageSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "LoadAsync cancels and awaits pending loads after an early failure")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenReportContextFailsCancelsAndAwaitsPendingLoads()
    {
        // Arrange
        var settings = CreateSettings(null, null);
        using var cts = new CancellationTokenSource();
        var pendingLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        dataFacade.Setup(facade => facade.LoadReportContextAsync(
                settings,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Invalid report context."));
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 0),
                It.IsAny<CancellationToken>()))
            .Returns(async (AppSettings _, IReadOnlyList<IssueTypeName> _, CancellationToken token) =>
            {
                pendingLoadStarted.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    cancellationObserved.SetResult();
                    await allowCleanup.Task;
                }

                return CreateRatioSnapshot(3);
            });

        var sut = new JiraApplicationReportLoader(
            settings,
            dataFacade.Object);

        // Act
        var loadTask = sut.LoadAsync(cts.Token);
        await pendingLoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var firstCompletedTask = await Task.WhenAny(
            cancellationObserved.Task,
            loadTask).WaitAsync(TimeSpan.FromSeconds(5));
        var cancellationWasObservedBeforeReturn = firstCompletedTask == cancellationObserved.Task;
        var loaderAwaitedPendingCleanup = !loadTask.IsCompleted;

        if (!cancellationWasObservedBeforeReturn)
        {
            await cts.CancelAsync();
        }

        allowCleanup.SetResult();
        var result = await loadTask;

        // Assert
        result.Should().BeOfType<ReportLoadResult.Failure>();
        cancellationWasObservedBeforeReturn.Should().BeTrue();
        loaderAwaitedPendingCleanup.Should().BeTrue();
    }

    private static Mock<IJiraApplicationDataFacade> CreateDataFacade(
        AppSettings settings,
        IssueRatioSnapshot allTasksRatio)
    {
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        dataFacade.Setup(facade => facade.LoadReportContextAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraReportContext([], [], [], [], [], [], [], []));
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allTasksRatio);
        return dataFacade;
    }

    private static AppSettings CreateSettings(
        TestCoverageSettings? testCoverage,
        IReadOnlyList<IssueTypeName>? internalIncidentIssueNames) =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token-value"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            new StatusName("Rejected"),
            [new StageName("Code Review")],
            new MonthLabel("2026-03"),
            testCoverage: testCoverage,
            internalIncidentIssueNames: internalIncidentIssueNames);

    private static IssueRatioSnapshot CreateRatioSnapshot(int created) =>
        new(
            new ItemCount(created),
            new ItemCount(1),
            new ItemCount(1),
            new ItemCount(0),
            new ItemCount(1),
            [],
            [],
            []);
}
