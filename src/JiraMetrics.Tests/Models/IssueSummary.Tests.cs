using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueSummaryTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentNullException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new IssueSummary(value);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        // Arrange
        var value = "  Summary text  ";

        // Act
        var summary = new IssueSummary(value);

        // Assert
        summary.Value.Should().Be("Summary text");
    }

    [Fact(DisplayName = "ToString returns value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        // Arrange
        var summary = new IssueSummary("Summary text");

        // Act
        var text = summary.ToString();

        // Assert
        text.Should().Be("Summary text");
    }
}
