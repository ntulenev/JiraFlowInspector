using FluentAssertions;

using JiraMetrics.Helpers;

namespace JiraMetrics.Tests.Helpers;

public sealed class StringHelpersTests
{
    [Fact(DisplayName = "EscapeJqlString escapes backslashes and quotes")]
    [Trait("Category", "Unit")]
    public void EscapeJqlStringWhenCalledEscapesBackslashesAndQuotes()
    {
        // Arrange
        var value = "a\\b\"c";

        // Act
        var escaped = value.EscapeJqlString();

        // Assert
        escaped.Should().Be("a\\\\b\\\"c");
    }

    [Fact(DisplayName = "EscapeJqlString throws when value is null")]
    [Trait("Category", "Unit")]
    public void EscapeJqlStringWhenValueIsNullThrowsArgumentNullException()
    {
        // Arrange
        string value = null!;

        // Act
        Action act = () => _ = value.EscapeJqlString();

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "EscapeJqlString returns original string when no escaping needed")]
    [Trait("Category", "Unit")]
    public void EscapeJqlStringWhenNoEscapingNeededReturnsSameValue()
    {
        // Arrange
        var value = "simple-text";

        // Act
        var escaped = value.EscapeJqlString();

        // Assert
        escaped.Should().Be(value);
    }
}
