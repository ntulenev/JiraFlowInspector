using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTimelineTests
{
    [Fact(DisplayName = "Constructor throws when transitions are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransitionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<TransitionEvent> transitions = null!;

        // Act
        Action act = () => _ = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when end time is earlier than created time")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenEndTimeIsEarlierThanCreatedThrowsArgumentException()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow;
        var endTime = created.AddMinutes(-1);

        // Act
        Action act = () => _ = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            endTime,
            [],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-2);
        var endTime = DateTimeOffset.UtcNow;
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), endTime, TimeSpan.FromHours(2))
        };

        // Act
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            endTime,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        issue.Key.Value.Should().Be("AAA-1");
        issue.IssueType.Value.Should().Be("Story");
        issue.Summary.Value.Should().Be("Summary");
        issue.Created.Should().Be(created);
        issue.EndTime.Should().Be(endTime);
        issue.Transitions.Should().BeSameAs(transitions);
        issue.PathKey.Value.Should().Be("OPEN->DONE");
        issue.PathLabel.Value.Should().Be("Open -> Done");
    }
}
