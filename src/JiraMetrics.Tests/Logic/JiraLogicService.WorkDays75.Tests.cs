using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraLogicServiceWorkDays75Tests
{
    [Fact(DisplayName = "BuildDaysAtWork75PerType throws when issues are null")]
    [Trait("Category", "Unit")]
    public void BuildDaysAtWork75PerTypeWhenIssuesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new JiraLogicService(new JiraAnalyticsService());
        IReadOnlyList<IssueTimeline> issues = null!;

        // Act
        Action act = () => _ = service.BuildDaysAtWork75PerType(issues, new StatusName("Done"));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "BuildDaysAtWork75PerType groups by issue type and calculates p75")]
    [Trait("Category", "Unit")]
    public void BuildDaysAtWork75PerTypeWhenCalledReturnsP75PerType()
    {
        // Arrange
        var service = new JiraLogicService(new JiraAnalyticsService());
        var now = DateTimeOffset.UtcNow;

        var taskIssueOne = CreateIssue(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            now,
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("In Progress"), now.AddHours(1), TimeSpan.FromHours(1)),
                new TransitionEvent(new StatusName("In Progress"), new StatusName("Done"), now.AddHours(25), TimeSpan.FromHours(24))
            ]);
        var taskIssueTwo = CreateIssue(
            new IssueKey("AAA-2"),
            new IssueTypeName("Task"),
            now,
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("In Progress"), now.AddHours(2), TimeSpan.FromHours(2)),
                new TransitionEvent(new StatusName("In Progress"), new StatusName("Done"), now.AddHours(50), TimeSpan.FromHours(48))
            ]);
        var bugIssue = CreateIssue(
            new IssueKey("AAA-3"),
            new IssueTypeName("Bug"),
            now,
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("Done"), now.AddHours(12), TimeSpan.FromHours(12))
            ]);

        // Act
        var result = service.BuildDaysAtWork75PerType(
            [taskIssueOne, taskIssueTwo, bugIssue],
            new StatusName("Done"));

        // Assert
        result.Should().HaveCount(2);
        result[0].IssueType.Value.Should().Be("Task");
        result[0].IssueCount.Value.Should().Be(2);
        result[0].DaysAtWorkP75.TotalHours.Should().Be(43.75);
        result[1].IssueType.Value.Should().Be("Bug");
        result[1].IssueCount.Value.Should().Be(1);
        result[1].DaysAtWorkP75.TotalHours.Should().Be(12);
    }

    [Fact(DisplayName = "BuildDaysAtWork75PerType skips issues that did not reach target status")]
    [Trait("Category", "Unit")]
    public void BuildDaysAtWork75PerTypeWhenIssueDidNotReachTargetSkipsIt()
    {
        // Arrange
        var service = new JiraLogicService(new JiraAnalyticsService());
        var now = DateTimeOffset.UtcNow;
        var issue = CreateIssue(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            now,
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("In Progress"), now.AddHours(1), TimeSpan.FromHours(1))
            ]);

        // Act
        var result = service.BuildDaysAtWork75PerType([issue], new StatusName("Done"));

        // Assert
        result.Should().BeEmpty();
    }

    private static IssueTimeline CreateIssue(
        IssueKey key,
        IssueTypeName issueType,
        DateTimeOffset created,
        IReadOnlyList<TransitionEvent> transitions)
    {
        return new IssueTimeline(
            key,
            issueType,
            new IssueSummary($"Summary {key.Value}"),
            created,
            created.AddDays(2),
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }
}
