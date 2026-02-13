using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraLogicServiceTests
{
    [Fact(DisplayName = "Constructor throws when analytics service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAnalyticsServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraAnalyticsService analytics = null!;

        // Act
        Action act = () => _ = new JiraLogicService(analytics);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "FilterIssuesByRequiredStage returns only matching issues")]
    [Trait("Category", "Unit")]
    public void FilterIssuesByRequiredStageWhenStageIsProvidedReturnsOnlyMatches()
    {
        // Arrange
        var analytics = new JiraAnalyticsService();
        var service = new JiraLogicService(analytics);

        var matchingIssue = CreateIssue(
            new IssueKey("AAA-1"),
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
                new TransitionEvent(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
            ]);

        var nonMatchingIssue = CreateIssue(
            new IssueKey("AAA-2"),
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("In Progress"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
                new TransitionEvent(new StatusName("In Progress"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
            ]);

        // Act
        var result = service.FilterIssuesByRequiredStage([matchingIssue, nonMatchingIssue], new StageName("Code Review"));

        // Assert
        result.Should().ContainSingle();
        result[0].Key.Value.Should().Be("AAA-1");
    }

    [Fact(DisplayName = "BuildPathGroups groups by path and calculates p75 transitions")]
    [Trait("Category", "Unit")]
    public void BuildPathGroupsWhenIssuesSharePathBuildsSingleGroupWithP75()
    {
        // Arrange
        var analytics = new JiraAnalyticsService();
        var service = new JiraLogicService(analytics);

        var issueOne = CreateIssue(
            new IssueKey("AAA-1"),
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2)),
                new TransitionEvent(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(4))
            ]);

        var issueTwo = CreateIssue(
            new IssueKey("AAA-2"),
            [
                new TransitionEvent(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(6)),
                new TransitionEvent(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(8))
            ]);

        // Act
        var result = service.BuildPathGroups([issueOne, issueTwo]);

        // Assert
        result.Should().ContainSingle();
        result[0].Issues.Should().HaveCount(2);
        result[0].P75Transitions.Should().HaveCount(2);
        result[0].P75Transitions[0].P75Duration.Should().Be(TimeSpan.FromHours(5));
        result[0].P75Transitions[1].P75Duration.Should().Be(TimeSpan.FromHours(7));
        result[0].TotalP75.Should().Be(TimeSpan.FromHours(12));
    }

    [Fact(DisplayName = "BuildPathGroups orders groups by issue count then path label")]
    [Trait("Category", "Unit")]
    public void BuildPathGroupsWhenMultipleGroupsExistOrdersByCountThenLabel()
    {
        // Arrange
        var analyticsMock = new Mock<IJiraAnalyticsService>(MockBehavior.Strict);
        analyticsMock
            .Setup(x => x.CalculatePercentile(It.IsAny<IReadOnlyList<TimeSpan>>(), It.IsAny<PercentileValue>()))
            .Returns<IReadOnlyList<TimeSpan>, PercentileValue>((samples, _) => samples[0]);

        var service = new JiraLogicService(analyticsMock.Object);

        var groupA1 = CreateIssueWithPath(new IssueKey("AAA-1"), new PathKey("A"), new PathLabel("Alpha"));
        var groupA2 = CreateIssueWithPath(new IssueKey("AAA-2"), new PathKey("A"), new PathLabel("Alpha"));
        var groupB = CreateIssueWithPath(new IssueKey("AAA-3"), new PathKey("B"), new PathLabel("Beta"));

        // Act
        var result = service.BuildPathGroups([groupB, groupA1, groupA2]);

        // Assert
        result.Should().HaveCount(2);
        result[0].PathLabel.Value.Should().Be("Alpha");
        result[1].PathLabel.Value.Should().Be("Beta");
    }

    private static IssueTimeline CreateIssue(IssueKey key, IReadOnlyList<TransitionEvent> transitions)
    {
        return new IssueTimeline(
            key,
            new IssueTypeName("Story"),
            new IssueSummary($"Summary {key.Value}"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }

    private static IssueTimeline CreateIssueWithPath(IssueKey key, PathKey pathKey, PathLabel pathLabel)
    {
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
        };

        return new IssueTimeline(
            key,
            new IssueTypeName("Story"),
            new IssueSummary($"Summary {key.Value}"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            pathKey,
            pathLabel);
    }
}
