using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class AdditionalModelTypesTests
{
    [Fact(DisplayName = "ArchTaskItem reports resolution state and elapsed time")]
    [Trait("Category", "Unit")]
    public void ArchTaskItemWhenCreatedExposesResolutionStateAndElapsedTime()
    {
        var createdAt = new DateTimeOffset(2026, 03, 01, 8, 0, 0, TimeSpan.Zero);
        var resolvedAt = createdAt.AddHours(5);

        var resolved = new ArchTaskItem(
            new IssueKey("ARCH-1"),
            new IssueSummary("Task"),
            createdAt,
            resolvedAt);
        var unresolved = new ArchTaskItem(
            new IssueKey("ARCH-2"),
            new IssueSummary("Task"),
            createdAt);

        resolved.IsResolved.Should().BeTrue();
        resolved.GetElapsed(createdAt.AddDays(1)).Should().Be(TimeSpan.FromHours(5));
        unresolved.IsResolved.Should().BeFalse();
        unresolved.GetElapsed(createdAt.AddHours(2)).Should().Be(TimeSpan.FromHours(2));
    }

    [Fact(DisplayName = "ArchTaskItem clamps elapsed duration to zero")]
    [Trait("Category", "Unit")]
    public void ArchTaskItemWhenFinishTimeIsEarlierThanCreatedReturnsZeroElapsed()
    {
        var createdAt = new DateTimeOffset(2026, 03, 01, 8, 0, 0, TimeSpan.Zero);
        var resolvedAt = createdAt.AddMinutes(-10);

        var item = new ArchTaskItem(
            new IssueKey("ARCH-1"),
            new IssueSummary("Task"),
            createdAt,
            resolvedAt);

        item.GetElapsed(createdAt.AddHours(1)).Should().Be(TimeSpan.Zero);
    }

    [Fact(DisplayName = "Simple model records set expected properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsPropertiesForAdditionalModelTypes()
    {
        var issue = new IssueListItem(new IssueKey("AAA-1"), new IssueSummary("Summary"), DateTimeOffset.UtcNow);
        var doneIssue = CreateIssueTimeline("AAA-1");
        var rejectedIssue = CreateIssueTimeline("AAA-2");
        var failure = new LoadFailure(new IssueKey("AAA-3"), new ErrorMessage("failure"));
        var endpoint = new JiraRequestTelemetryEndpointSummary(
            "GET",
            "/rest/api/3/search",
            2,
            1,
            512,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(2));

        var searchSnapshot = new IssueSearchSnapshot([issue], [issue], []);
        var ratioSnapshot = new IssueRatioSnapshot(
            new ItemCount(1),
            new ItemCount(2),
            new ItemCount(3),
            new ItemCount(4),
            new ItemCount(5),
            [issue],
            [issue],
            []);
        var batchResult = new IssueTimelineBatchResult([doneIssue], [failure]);
        var loadResult = new IssueTimelineLoadResult([doneIssue], [rejectedIssue], [failure], new ItemCount(2));
        var environmentFilter = new ReleaseEnvironmentFilter(
            new JiraFieldName("Environment"),
            new JiraFieldValue("Production"));
        var telemetrySummary = new JiraRequestTelemetrySummary(
            3,
            1,
            1024,
            TimeSpan.FromSeconds(4),
            [endpoint]);

        searchSnapshot.CreatedIssues.Should().Equal(issue);
        searchSnapshot.DoneIssues.Should().Equal(issue);
        searchSnapshot.RejectedIssues.Should().BeEmpty();

        ratioSnapshot.CreatedThisMonth.Should().Be(new ItemCount(1));
        ratioSnapshot.OpenThisMonth.Should().Be(new ItemCount(2));
        ratioSnapshot.MovedToDoneThisMonth.Should().Be(new ItemCount(3));
        ratioSnapshot.RejectedThisMonth.Should().Be(new ItemCount(4));
        ratioSnapshot.FinishedThisMonth.Should().Be(new ItemCount(5));
        ratioSnapshot.OpenIssues.Should().Equal(issue);
        ratioSnapshot.DoneIssues.Should().Equal(issue);
        ratioSnapshot.RejectedIssues.Should().BeEmpty();

        batchResult.Issues.Should().Equal(doneIssue);
        batchResult.Failures.Should().Equal(failure);

        loadResult.DoneIssues.Should().Equal(doneIssue);
        loadResult.RejectIssues.Should().Equal(rejectedIssue);
        loadResult.Failures.Should().Equal(failure);
        loadResult.LoadedIssueCount.Should().Be(new ItemCount(2));

        environmentFilter.FieldName.Should().Be(new JiraFieldName("Environment"));
        environmentFilter.Value.Should().Be(new JiraFieldValue("Production"));

        telemetrySummary.RequestCount.Should().Be(3);
        telemetrySummary.RetryCount.Should().Be(1);
        telemetrySummary.ResponseBytes.Should().Be(1024);
        telemetrySummary.TotalDuration.Should().Be(TimeSpan.FromSeconds(4));
        telemetrySummary.Endpoints.Should().Equal(endpoint);

        endpoint.Method.Should().Be("GET");
        endpoint.Endpoint.Should().Be("/rest/api/3/search");
        endpoint.RequestCount.Should().Be(2);
        endpoint.RetryCount.Should().Be(1);
        endpoint.ResponseBytes.Should().Be(512);
        endpoint.TotalDuration.Should().Be(TimeSpan.FromSeconds(3));
        endpoint.MaxDuration.Should().Be(TimeSpan.FromSeconds(2));
    }

    private static IssueTimeline CreateIssueTimeline(string key)
    {
        var created = new DateTimeOffset(2026, 03, 01, 8, 0, 0, TimeSpan.Zero);
        var finished = created.AddHours(2);

        return new IssueTimeline(
            new IssueKey(key),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            finished,
            [],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));
    }
}
