using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class DurationLabelTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new DurationLabel(value);

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
        Action act = () => _ = new DurationLabel(value);

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
        var label = new DurationLabel(value);

        // Assert
        label.Value.Should().Be("sample");
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var label = new DurationLabel("sample");

        // Act
        var text = label.ToString();

        // Assert
        text.Should().Be("sample");
    }

    [Fact(DisplayName = "FromDuration returns expected text for mixed duration")]
    [Trait("Category", "Unit")]
    public void FromDurationWhenDurationHasDaysHoursAndMinutesReturnsExpectedText()
    {
        // Arrange
        var duration = new TimeSpan(days: 1, hours: 2, minutes: 30, seconds: 45);

        // Act
        var result = DurationLabel.FromDuration(duration);

        // Assert
        result.Value.Should().Be("1d 2h 30m");
    }

    [Fact(DisplayName = "FromDuration returns seconds when no larger units exist")]
    [Trait("Category", "Unit")]
    public void FromDurationWhenOnlySecondsExistReturnsSecondsText()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(9);

        // Act
        var result = DurationLabel.FromDuration(duration);

        // Assert
        result.Value.Should().Be("9s");
    }

    [Fact(DisplayName = "FromDuration normalizes negative duration to zero")]
    [Trait("Category", "Unit")]
    public void FromDurationWhenDurationIsNegativeReturnsZeroSeconds()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(-5);

        // Act
        var result = DurationLabel.FromDuration(duration);

        // Assert
        result.Value.Should().Be("0s");
    }
}
