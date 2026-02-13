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

    [Fact(DisplayName = "Truncate returns same summary when length fits")]
    [Trait("Category", "Unit")]
    public void TruncateWhenSummaryFitsReturnsSameSummary()
    {
        // Arrange
        var summary = new IssueSummary("Short");

        // Act
        var truncated = summary.Truncate(new TextLength(10));

        // Assert
        truncated.Should().Be(summary);
    }

    [Fact(DisplayName = "Truncate shortens summary and adds ellipsis when length exceeds maximum")]
    [Trait("Category", "Unit")]
    public void TruncateWhenSummaryExceedsLengthReturnsEllipsisSummary()
    {
        // Arrange
        var summary = new IssueSummary("This is a long summary text");

        // Act
        var truncated = summary.Truncate(new TextLength(10));

        // Assert
        truncated.Value.Should().Be("This is...");
    }
}
