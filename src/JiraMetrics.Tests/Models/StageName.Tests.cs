using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class StageNameTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new StageName(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when value is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        // Arrange
        var value = "   ";

        // Act
        Action act = () => _ = new StageName(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        // Arrange
        var value = "  sample  ";

        // Act
        var stageName = new StageName(value);

        // Assert
        stageName.Value.Should().Be("sample");
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var stageName = new StageName("sample");

        // Act
        var text = stageName.ToString();

        // Assert
        text.Should().Be("sample");
    }

    [Fact(DisplayName = "IsUsedInTransition returns true when stage matches from or to")]
    [Trait("Category", "Unit")]
    public void IsUsedInTransitionWhenStageMatchesReturnsTrue()
    {
        // Arrange
        var stage = new StageName("code review");
        var transition = new TransitionEvent(
            new StatusName("Open"),
            new StatusName("Code Review"),
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(1));

        // Act
        var result = stage.IsUsedInTransition(transition);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "IsUsedInTransition returns false when stage does not match")]
    [Trait("Category", "Unit")]
    public void IsUsedInTransitionWhenStageDoesNotMatchReturnsFalse()
    {
        // Arrange
        var stage = new StageName("blocked");
        var transition = new TransitionEvent(
            new StatusName("Open"),
            new StatusName("Done"),
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(1));

        // Act
        var result = stage.IsUsedInTransition(transition);

        // Assert
        result.Should().BeFalse();
    }
}
