using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTypeNameTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new IssueTypeName(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        // Arrange
        var value = "  Story  ";

        // Act
        var issueType = new IssueTypeName(value);

        // Assert
        issueType.Value.Should().Be("Story");
    }

    [Fact(DisplayName = "Unknown returns unknown issue type")]
    [Trait("Category", "Unit")]
    public void UnknownWhenRequestedReturnsUnknownIssueType()
    {
        // Arrange

        // Act
        var issueType = IssueTypeName.Unknown;

        // Assert
        issueType.Value.Should().Be("Unknown");
    }

    [Fact(DisplayName = "FromNullable returns unknown when value is null or whitespace")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsNullOrWhiteSpaceReturnsUnknown()
    {
        // Arrange

        // Act
        var nullType = IssueTypeName.FromNullable(null);
        var whitespaceType = IssueTypeName.FromNullable("   ");

        // Assert
        nullType.Should().Be(IssueTypeName.Unknown);
        whitespaceType.Should().Be(IssueTypeName.Unknown);
    }

    [Fact(DisplayName = "FromNullable returns trimmed issue type when value is provided")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsProvidedReturnsTrimmedIssueType()
    {
        // Arrange

        // Act
        var issueType = IssueTypeName.FromNullable("  Bug  ");

        // Assert
        issueType.Value.Should().Be("Bug");
    }
}
