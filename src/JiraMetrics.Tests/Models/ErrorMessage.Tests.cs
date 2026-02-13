using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class ErrorMessageTests
{
    [Fact(DisplayName = "Constructor throws when value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = new ErrorMessage(value);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        // Arrange
        var value = "  boom  ";

        // Act
        var error = new ErrorMessage(value);

        // Assert
        error.Value.Should().Be("boom");
    }

    [Fact(DisplayName = "FromException throws when exception is null")]
    [Trait("Category", "Unit")]
    public void FromExceptionWhenExceptionIsNullThrowsArgumentNullException()
    {
        // Arrange
        Exception exception = null!;

        // Act
        Action act = () => _ = ErrorMessage.FromException(exception);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "FromException returns unknown error when message is empty")]
    [Trait("Category", "Unit")]
    public void FromExceptionWhenMessageIsEmptyReturnsUnknownError()
    {
        // Arrange
        var exception = new InvalidOperationException(string.Empty);

        // Act
        var error = ErrorMessage.FromException(exception);

        // Assert
        error.Value.Should().Be("Unknown error.");
    }

    [Fact(DisplayName = "FromException returns exception message")]
    [Trait("Category", "Unit")]
    public void FromExceptionWhenMessageIsProvidedReturnsMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Failed.");

        // Act
        var error = ErrorMessage.FromException(exception);

        // Assert
        error.Value.Should().Be("Failed.");
    }
}
