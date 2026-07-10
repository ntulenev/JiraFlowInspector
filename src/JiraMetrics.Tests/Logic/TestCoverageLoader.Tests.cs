using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class TestCoverageLoaderTests
{
    [Fact(DisplayName = "LoadAsync counts done issues linked to configured QA project by configured relation")]
    [Trait("Category", "Unit")]
    public async Task LoadAsyncWhenDoneIssueHasConfiguredQaLinkCountsCoverage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var settings = CreateSettings();
        var coverageSettings = new TestCoverageSettings(
            issueTypes: [new IssueTypeName("SuperTask")],
            testProjectKey: new ProjectKey("QA"),
            linkName: "is tested by");
        var issueSearchClient = new FakeIssueSearchClient
        {
            DoneIssues =
            [
                new IssueListItem(
                    new IssueKey("ADF-1"),
                    new IssueSummary("Covered task"),
                    issueLinks: [new IssueLinkItem(new IssueKey("QA-1"), "is tested by")]),
                new IssueListItem(
                    new IssueKey("ADF-2"),
                    new IssueSummary("Uncovered task"),
                    issueLinks: [new IssueLinkItem(new IssueKey("BUG-1"), "is tested by")])
            ]
        };

        var loader = new TestCoverageLoader(issueSearchClient);

        // Act
        var snapshot = await loader.LoadAsync(settings, coverageSettings, cts.Token);

        // Assert
        snapshot.TotalIssues.Value.Should().Be(2);
        snapshot.CoveredIssueCount.Value.Should().Be(1);
        snapshot.CoveragePercentage.Should().Be(50);
        issueSearchClient.IncludeIssueLinksRequested.Should().BeTrue();
    }

    private static AppSettings CreateSettings() =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("ADF"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-02"));

    private sealed class FakeIssueSearchClient : IJiraIssueSearchClient
    {
        public IReadOnlyList<IssueListItem> DoneIssues { get; init; } = [];

        public bool IncludeIssueLinksRequested { get; private set; }

        public Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            CreatedAfterDate? createdAfter,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IssueKey>>([]);

        public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null) =>
            Task.FromResult<IReadOnlyList<IssueListItem>>([]);

        public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken,
            JiraFieldName? reporducedOnProdFieldName = null,
            bool includeIssueLinks = false)
        {
            IncludeIssueLinksRequested = includeIssueLinks;
            return Task.FromResult(DoneIssues);
        }

        public Task<IReadOnlyList<StatusIssueTypeSummary>> GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            StatusName? rejectStatusName,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StatusIssueTypeSummary>>([]);
    }
}
