using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraBaseUrlTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new JiraBaseUrl(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when value is not an absolute URI")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNotAbsoluteUriThrowsArgumentException()
    {
        // Arrange
        var value = "/relative";

        // Act
        Action act = () => _ = new JiraBaseUrl(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor normalizes and trims trailing slash")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsValidNormalizesValue()
    {
        // Arrange
        var value = " https://example.atlassian.net/ ";

        // Act
        var baseUrl = new JiraBaseUrl(value);

        // Assert
        baseUrl.Value.Should().Be("https://example.atlassian.net");
    }
}
