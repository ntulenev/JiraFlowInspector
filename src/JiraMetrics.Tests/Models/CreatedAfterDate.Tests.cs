using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class CreatedAfterDateTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new CreatedAfterDate(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when value is not yyyy-MM-dd")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueHasInvalidFormatThrowsArgumentException()
    {
        // Arrange
        var value = "2026/01/15";

        // Act
        Action act = () => _ = new CreatedAfterDate(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor sets parsed date value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsValidSetsDateValue()
    {
        // Arrange
        var value = "2026-01-15";

        // Act
        var createdAfterDate = new CreatedAfterDate(value);

        // Assert
        createdAfterDate.Value.Should().Be(new DateOnly(2026, 1, 15));
    }

    [Fact(DisplayName = "ToString returns yyyy-MM-dd")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsInvariantDateText()
    {
        // Arrange
        var createdAfterDate = new CreatedAfterDate("2026-01-15");

        // Act
        var text = createdAfterDate.ToString();

        // Assert
        text.Should().Be("2026-01-15");
    }
}
