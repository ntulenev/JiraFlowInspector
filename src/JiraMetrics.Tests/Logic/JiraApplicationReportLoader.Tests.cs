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
    [Fact(DisplayName = "TryLoadAsync returns internal incidents and test coverage when optional loads succeed")]
    [Trait("Category", "Unit")]
    public async Task TryLoadAsyncWhenOptionalLoadsSucceedReturnsTheirData()
    {
        // Arrange
        var coverageSettings = new TestCoverageSettings();
        var settings = CreateSettings(coverageSettings, [new IssueTypeName("Incident")]);
        var allTasksRatio = CreateRatioSnapshot(3);
        var internalIncidents = CreateRatioSnapshot(2);
        var testCoverage = new TestCoverageSnapshot([], []);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, allTasksRatio, cts.Token);
        var reportingFacade = CreateReportingFacade();

        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 1 && types[0].Value == "Incident"),
                cts.Token))
            .ReturnsAsync(internalIncidents);
        dataFacade.Setup(facade => facade.LoadTestCoverageAsync(
                settings,
                coverageSettings,
                cts.Token))
            .ReturnsAsync(testCoverage);

        var sut = new JiraApplicationReportLoader(settings, dataFacade.Object, reportingFacade.Object);

        // Act
        var result = await sut.TryLoadAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result!.InternalIncidents.Should().BeSameAs(internalIncidents);
        result.TestCoverage.Should().BeSameAs(testCoverage);
        reportingFacade.Verify(facade => facade.ShowTestCoverage(coverageSettings, testCoverage), Times.Once);
    }

    [Fact(DisplayName = "TryLoadAsync stops when internal-incident loading fails")]
    [Trait("Category", "Unit")]
    public async Task TryLoadAsyncWhenInternalIncidentLoadFailsReturnsNull()
    {
        // Arrange
        var settings = CreateSettings(null, [new IssueTypeName("Incident")]);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3), cts.Token);
        var reportingFacade = CreateReportingFacade();

        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 1),
                cts.Token))
            .ThrowsAsync(new JsonException("Invalid incident response."));

        var sut = new JiraApplicationReportLoader(settings, dataFacade.Object, reportingFacade.Object);

        // Act
        var result = await sut.TryLoadAsync(cts.Token);

        // Assert
        result.Should().BeNull();
        reportingFacade.Verify(
            facade => facade.ShowIssueSearchFailed(It.Is<ErrorMessage>(message =>
                message.Value.Contains("Invalid incident response.", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact(DisplayName = "TryLoadAsync stops when test-coverage loading fails")]
    [Trait("Category", "Unit")]
    public async Task TryLoadAsyncWhenTestCoverageLoadFailsReturnsNull()
    {
        // Arrange
        var coverageSettings = new TestCoverageSettings();
        var settings = CreateSettings(coverageSettings, null);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3), cts.Token);
        var reportingFacade = CreateReportingFacade();

        dataFacade.Setup(facade => facade.LoadTestCoverageAsync(
                settings,
                coverageSettings,
                cts.Token))
            .ThrowsAsync(new HttpRequestException("Coverage service unavailable."));

        var sut = new JiraApplicationReportLoader(settings, dataFacade.Object, reportingFacade.Object);

        // Act
        var result = await sut.TryLoadAsync(cts.Token);

        // Assert
        result.Should().BeNull();
        reportingFacade.Verify(
            facade => facade.ShowIssueSearchFailed(It.Is<ErrorMessage>(message =>
                message.Value.Contains("Coverage service unavailable.", StringComparison.Ordinal))),
            Times.Once);
        reportingFacade.Verify(
            facade => facade.ShowTestCoverage(It.IsAny<TestCoverageSettings>(), It.IsAny<TestCoverageSnapshot>()),
            Times.Never);
    }

    [Fact(DisplayName = "TryLoadAsync skips test-coverage loading when the feature is disabled")]
    [Trait("Category", "Unit")]
    public async Task TryLoadAsyncWhenTestCoverageIsDisabledSkipsCoverageRequest()
    {
        // Arrange
        var settings = CreateSettings(new TestCoverageSettings(enabled: false), null);
        using var cts = new CancellationTokenSource();
        var dataFacade = CreateDataFacade(settings, CreateRatioSnapshot(3), cts.Token);
        var reportingFacade = CreateReportingFacade();
        var sut = new JiraApplicationReportLoader(settings, dataFacade.Object, reportingFacade.Object);

        // Act
        var result = await sut.TryLoadAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result!.TestCoverage.Should().BeSameAs(TestCoverageSnapshot.Empty);
        dataFacade.Verify(
            facade => facade.LoadTestCoverageAsync(
                It.IsAny<AppSettings>(),
                It.IsAny<TestCoverageSettings>(),
                cts.Token),
            Times.Never);
    }

    private static Mock<IJiraApplicationDataFacade> CreateDataFacade(
        AppSettings settings,
        IssueRatioSnapshot allTasksRatio,
        CancellationToken cancellationToken)
    {
        var dataFacade = new Mock<IJiraApplicationDataFacade>(MockBehavior.Strict);
        dataFacade.Setup(facade => facade.LoadReportContextAsync(settings, cancellationToken))
            .ReturnsAsync(new JiraReportContext([], [], [], [], [], [], []));
        dataFacade.Setup(facade => facade.LoadIssueRatioAsync(
                settings,
                It.Is<IReadOnlyList<IssueTypeName>>(types => types.Count == 0),
                cancellationToken))
            .ReturnsAsync(allTasksRatio);
        return dataFacade;
    }

    private static Mock<IJiraApplicationReportingFacade> CreateReportingFacade()
    {
        var reportingFacade = new Mock<IJiraApplicationReportingFacade>(MockBehavior.Strict);
        reportingFacade.Setup(facade => facade.ShowReportPeriodContext(
            It.IsAny<ReportPeriod>(),
            It.IsAny<CreatedAfterDate?>()));
        reportingFacade.Setup(facade => facade.ShowSpacer());
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
        reportingFacade.Setup(facade => facade.ShowTestCoverageLoadingStarted(It.IsAny<TestCoverageSettings>()));
        reportingFacade.Setup(facade => facade.ShowTestCoverage(
            It.IsAny<TestCoverageSettings>(),
            It.IsAny<TestCoverageSnapshot>()));
        reportingFacade.Setup(facade => facade.ShowIssueSearchFailed(It.IsAny<ErrorMessage>()));
        return reportingFacade;
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
