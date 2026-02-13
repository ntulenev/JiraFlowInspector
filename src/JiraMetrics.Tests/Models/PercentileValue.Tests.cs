using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PercentileValueTests
{
    [Fact(DisplayName = "Constructor throws when value is NaN")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNaNThrowsArgumentOutOfRangeException()
    {
        // Arrange

        // Act
        Action act = () => _ = new PercentileValue(double.NaN);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor throws when value is infinity")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsInfinityThrowsArgumentOutOfRangeException()
    {
        // Arrange

        // Act
        Action act = () => _ = new PercentileValue(double.PositiveInfinity);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "Constructor throws when value is out of range")]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsOutOfRangeThrowsArgumentOutOfRangeException(double value)
    {
        // Arrange

        // Act
        Action act = () => _ = new PercentileValue(value);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets value when value is in range")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsInRangeSetsValue()
    {
        // Arrange
        var value = 0.75;

        // Act
        var percentile = new PercentileValue(value);

        // Assert
        percentile.Value.Should().Be(value);
    }

    [Fact(DisplayName = "ToString formats value with up to three decimals")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledFormatsValue()
    {
        // Arrange
        var percentile = new PercentileValue(0.7564);

        // Act
        var text = percentile.ToString();

        // Assert
        text.Should().Be("0.756");
    }
}
