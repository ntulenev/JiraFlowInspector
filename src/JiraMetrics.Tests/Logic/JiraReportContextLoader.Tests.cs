using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraReportContextLoaderTests
{
    [Fact(DisplayName = "LoadAsync records an enabled optional section failure without losing required data")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenOptionalSectionFailsReturnsPartialContext()
    {
        // Arrange
        var settings = CreateSettings(new ArchTasksReportSettings("project = ARCH"));
        var issueSearchClient = new StubIssueSearchClient();
        var reportDataClient = new StubReportDataClient
        {
            ArchTasksLoader = static (_, _) =>
                Task.FromException<IReadOnlyList<ArchTaskItem>>(
                    new HttpRequestException("Architecture project is unavailable."))
        };
        var sut = new JiraReportContextLoader(issueSearchClient, reportDataClient);

        // Act
        var result = await sut.LoadAsync(settings, CancellationToken.None);

        // Assert
        result.IssueKeys.Should().ContainSingle().Which.Should().Be(new IssueKey("APP-1"));
        result.ArchTasks.Should().BeEmpty();
        var failure = result.OptionalSectionFailures.Should().ContainSingle().Which;
        failure.Section.Should().Be(OptionalReportSection.ArchTasksReport);
        failure.Error.Value.Should().Be("Architecture project is unavailable.");
    }

    [Fact(DisplayName = "LoadAsync propagates unexpected optional section failures")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenOptionalSectionHasProgrammingFailurePropagatesException()
    {
        // Arrange
        var settings = CreateSettings(new ArchTasksReportSettings("project = ARCH"));
        var issueSearchClient = new StubIssueSearchClient();
        var reportDataClient = new StubReportDataClient
        {
            ArchTasksLoader = static (_, _) =>
                Task.FromException<IReadOnlyList<ArchTaskItem>>(
                    new InvalidOperationException("Broken mapping invariant."))
        };
        var sut = new JiraReportContextLoader(issueSearchClient, reportDataClient);

        // Act
        Func<Task> act = () => sut.LoadAsync(settings, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Broken mapping invariant.");
    }

    [Fact(DisplayName = "LoadAsync does not invoke disabled optional sections")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenOptionalSectionsAreDisabledSkipsTheirClients()
    {
        // Arrange
        var settings = CreateSettings(archTasksReport: null);
        var issueSearchClient = new StubIssueSearchClient();
        var reportDataClient = new StubReportDataClient();
        var sut = new JiraReportContextLoader(issueSearchClient, reportDataClient);

        // Act
        var result = await sut.LoadAsync(settings, CancellationToken.None);

        // Assert
        result.OptionalSectionFailures.Should().BeEmpty();
        reportDataClient.ArchTasksCallCount.Should().Be(0);
    }

    private sealed class StubIssueSearchClient : IJiraIssueSearchClient
    {
        public Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            CreatedAfterDate? createdAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssueKey>>([new IssueKey("APP-1")]);

        public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null,
            bool includeIssueLinks = false) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            StatusName? rejectStatusName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubReportDataClient : IJiraReportDataClient
    {
        public Func<ArchTasksReportSettings, CancellationToken, Task<IReadOnlyList<ArchTaskItem>>>?
            ArchTasksLoader { get; init; }

        public int ArchTasksCallCount { get; private set; }

        public Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
            ArchTasksReportSettings settings,
            CancellationToken cancellationToken)
        {
            ArchTasksCallCount++;
            return ArchTasksLoader is null
                ? throw new InvalidOperationException("Architecture-task loading was not expected.")
                : ArchTasksLoader(settings, cancellationToken);
        }

        public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
            ReleaseIssueReadRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<IssueListItem>> GetUnresolved30DaysTasksAsync(
            Unresolved30DaysTasksReportSettings settings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RoadmapItem>> GetRoadmapItemsAsync(
            RoadmapReportSettings settings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
            GlobalIncidentsReportSettings settings,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static AppSettings CreateSettings(ArchTasksReportSettings? archTasksReport) =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token-value"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-03"),
            showGeneralStatistics: false,
            archTasksReport: archTasksReport);
}
