using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PathGroupTests
{
    [Fact(DisplayName = "Constructor throws when issues are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenIssuesAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<IssueTimeline> issues = null!;

        // Act
        Action act = () => _ = new PathGroup(
            new PathLabel("Open -> Done"),
            issues,
            [],
            TimeSpan.Zero);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when p75 transitions are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenP75TransitionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<PercentileTransition> p75Transitions = null!;

        // Act
        Action act = () => _ = new PathGroup(
            new PathLabel("Open -> Done"),
            [],
            p75Transitions,
            TimeSpan.Zero);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var issues = new List<IssueTimeline>();
        var transitions = new List<PercentileTransition>
        {
            new(new StatusName("Open"), new StatusName("Done"), TimeSpan.FromHours(2))
        };

        // Act
        var group = new PathGroup(new PathLabel("Open -> Done"), issues, transitions, TimeSpan.FromHours(2));

        // Assert
        group.PathLabel.Value.Should().Be("Open -> Done");
        group.Issues.Should().BeSameAs(issues);
        group.P75Transitions.Should().BeSameAs(transitions);
        group.TotalP75.Should().Be(TimeSpan.FromHours(2));
    }
}
