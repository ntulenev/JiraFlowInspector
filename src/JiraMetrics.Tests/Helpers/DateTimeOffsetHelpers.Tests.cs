using FluentAssertions;

using JiraMetrics.Helpers;

namespace JiraMetrics.Tests.Helpers;

public sealed class DateTimeOffsetHelpersTests
{
    [Fact(DisplayName = "ParseNullableDateTimeOffset returns null for blank input")]
    [Trait("Category", "Unit")]
    public void ParseNullableDateTimeOffsetWhenInputIsBlankReturnsNull()
    {
        // Act
        var parsed = " ".ParseNullableDateTimeOffset();

        // Assert
        parsed.Should().BeNull();
    }

    [Fact(DisplayName = "ParseNullableDateTimeOffset returns parsed value for valid input")]
    [Trait("Category", "Unit")]
    public void ParseNullableDateTimeOffsetWhenInputIsValidReturnsParsedValue()
    {
        // Act
        var parsed = "2026-03-16T10:30:00+00:00".ParseNullableDateTimeOffset();

        // Assert
        parsed.Should().Be(new DateTimeOffset(2026, 3, 16, 10, 30, 0, TimeSpan.Zero));
    }

    [Fact(DisplayName = "ParseNullableDateTimeOffset returns null for invalid input")]
    [Trait("Category", "Unit")]
    public void ParseNullableDateTimeOffsetWhenInputIsInvalidReturnsNull()
    {
        // Act
        var parsed = "not-a-date".ParseNullableDateTimeOffset();

        // Assert
        parsed.Should().BeNull();
    }
}
