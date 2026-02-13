using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PathKeyTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new PathKey(value);

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
        Action act = () => _ = new PathKey(value);

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
        var pathKey = new PathKey(value);

        // Assert
        pathKey.Value.Should().Be("sample");
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var pathKey = new PathKey("sample");

        // Act
        var text = pathKey.ToString();

        // Assert
        text.Should().Be("sample");
    }

    [Fact(DisplayName = "FromTransitions returns no transitions key when list is empty")]
    [Trait("Category", "Unit")]
    public void FromTransitionsWhenTransitionsAreEmptyReturnsNoTransitionsKey()
    {
        // Arrange
        var transitions = Array.Empty<TransitionEvent>();

        // Act
        var pathKey = PathKey.FromTransitions(transitions);

        // Assert
        pathKey.Value.Should().Be("__NO_TRANSITIONS__");
    }

    [Fact(DisplayName = "FromTransitions builds uppercase combined key")]
    [Trait("Category", "Unit")]
    public void FromTransitionsWhenTransitionsExistReturnsCombinedKey()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("open"), new StatusName("code review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
            new(new StatusName("code review"), new StatusName("done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
        };

        // Act
        var pathKey = PathKey.FromTransitions(transitions);

        // Assert
        pathKey.Value.Should().Be("OPEN->CODE REVIEW||CODE REVIEW->DONE");
    }

    [Fact(DisplayName = "FromTransitions throws when transitions are null")]
    [Trait("Category", "Unit")]
    public void FromTransitionsWhenTransitionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<TransitionEvent> transitions = null!;

        // Act
        Action act = () => _ = PathKey.FromTransitions(transitions);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }
}
