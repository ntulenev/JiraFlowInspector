using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PathLabelTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new PathLabel(value);

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
        Action act = () => _ = new PathLabel(value);

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
        var pathLabel = new PathLabel(value);

        // Assert
        pathLabel.Value.Should().Be("sample");
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var pathLabel = new PathLabel("sample");

        // Act
        var text = pathLabel.ToString();

        // Assert
        text.Should().Be("sample");
    }

    [Fact(DisplayName = "FromTransitions returns no transitions label when list is empty")]
    [Trait("Category", "Unit")]
    public void FromTransitionsWhenTransitionsAreEmptyReturnsNoTransitionsLabel()
    {
        // Arrange
        var transitions = Array.Empty<TransitionEvent>();

        // Act
        var pathLabel = PathLabel.FromTransitions(transitions);

        // Assert
        pathLabel.Value.Should().Be("No transitions");
    }

    [Fact(DisplayName = "FromTransitions builds expected joined path label")]
    [Trait("Category", "Unit")]
    public void FromTransitionsWhenTransitionsExistReturnsJoinedLabel()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };

        // Act
        var pathLabel = PathLabel.FromTransitions(transitions);

        // Assert
        pathLabel.Value.Should().Be("Open -> Code Review | Code Review -> Done");
    }
}
