using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class TextLengthTests
{
    [Fact(DisplayName = "Constructor throws when value is less than four")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsLessThanFourThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = 3;

        // Act
        Action act = () => _ = new TextLength(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets value when at least four")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsAtLeastFourSetsValue()
    {
        // Arrange
        var value = 4;

        // Act
        var textLength = new TextLength(value);

        // Assert
        textLength.Value.Should().Be(value);
    }

    [Fact(DisplayName = "ToString returns numeric text")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsNumericText()
    {
        // Arrange
        var textLength = new TextLength(12);

        // Act
        var text = textLength.ToString();

        // Assert
        text.Should().Be("12");
    }
}
