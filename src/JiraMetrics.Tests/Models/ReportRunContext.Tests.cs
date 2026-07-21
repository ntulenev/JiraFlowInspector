using FluentAssertions;

using JiraMetrics.Models;

namespace JiraMetrics.Tests.Models;

public sealed class ReportRunContextTests
{
    [Fact(DisplayName = "Create captures local time from the supplied provider once")]
    [Trait("Category", "Unit")]
    public void CreateWhenTimeProviderIsSuppliedCapturesItsLocalTime()
    {
        // Arrange
        var utcNow = new DateTimeOffset(2026, 2, 3, 21, 59, 58, TimeSpan.Zero);
        var timeProvider = new FixedTimeProvider(utcNow, TimeZoneInfo.CreateCustomTimeZone(
            "Test UTC+2",
            TimeSpan.FromHours(2),
            "Test UTC+2",
            "Test UTC+2"));

        // Act
        var context = ReportRunContext.Create(timeProvider);

        // Assert
        context.GeneratedAt.Should().Be(
            new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2)));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone)
        {
            _utcNow = utcNow;
            LocalTimeZone = localTimeZone;
        }

        public override TimeZoneInfo LocalTimeZone { get; }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        private readonly DateTimeOffset _utcNow;
    }
}
