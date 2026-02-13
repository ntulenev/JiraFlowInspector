using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class StatusNameTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new StatusName(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        // Arrange
        var value = "  Done  ";

        // Act
        var status = new StatusName(value);

        // Assert
        status.Value.Should().Be("Done");
    }

    [Fact(DisplayName = "Unknown returns unknown status")]
    [Trait("Category", "Unit")]
    public void UnknownWhenRequestedReturnsUnknownStatus()
    {
        // Arrange

        // Act
        var status = StatusName.Unknown;

        // Assert
        status.Value.Should().Be("Unknown");
    }

    [Fact(DisplayName = "FromNullable returns unknown when value is null or whitespace")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsNullOrWhiteSpaceReturnsUnknown()
    {
        // Arrange

        // Act
        var nullStatus = StatusName.FromNullable(null);
        var whitespaceStatus = StatusName.FromNullable("   ");

        // Assert
        nullStatus.Should().Be(StatusName.Unknown);
        whitespaceStatus.Should().Be(StatusName.Unknown);
    }

    [Fact(DisplayName = "FromNullable returns trimmed status when value is provided")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsProvidedReturnsTrimmedStatus()
    {
        // Arrange

        // Act
        var status = StatusName.FromNullable("  In Progress  ");

        // Assert
        status.Value.Should().Be("In Progress");
    }
}
