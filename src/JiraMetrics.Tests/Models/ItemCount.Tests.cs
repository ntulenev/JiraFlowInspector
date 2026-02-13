using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class ItemCountTests
{
    [Fact(DisplayName = "Constructor throws when value is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = -1;

        // Act
        Action act = () => _ = new ItemCount(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets value when non-negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNonNegativeSetsValue()
    {
        // Arrange
        var value = 5;

        // Act
        var count = new ItemCount(value);

        // Assert
        count.Value.Should().Be(value);
    }

    [Fact(DisplayName = "ToString returns numeric text")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsNumericText()
    {
        // Arrange
        var count = new ItemCount(7);

        // Act
        var text = count.ToString();

        // Assert
        text.Should().Be("7");
    }
}
