using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class MonthLabelTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new MonthLabel(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when value does not match yyyy-MM")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueHasInvalidFormatThrowsArgumentException()
    {
        // Arrange
        var value = "2026/02";

        // Act
        Action act = () => _ = new MonthLabel(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor sets value when format is valid")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueHasValidFormatSetsValue()
    {
        // Arrange
        var value = "2026-02";

        // Act
        var monthLabel = new MonthLabel(value);

        // Assert
        monthLabel.Value.Should().Be(value);
    }

    [Fact(DisplayName = "CurrentUtc returns yyyy-MM formatted month")]
    [Trait("Category", "Unit")]
    public void CurrentUtcWhenCalledReturnsFormattedMonth()
    {
        // Arrange

        // Act
        var monthLabel = MonthLabel.CurrentUtc();

        // Assert
        monthLabel.Value.Should().MatchRegex("^\\d{4}-\\d{2}$");
    }
}
