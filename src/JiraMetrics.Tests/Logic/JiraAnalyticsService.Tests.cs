using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraAnalyticsServiceTests
{

    [Fact(DisplayName = "CalculatePercentile returns zero when values are empty")]
    [Trait("Category", "Unit")]
    public void CalculatePercentileWhenValuesAreEmptyReturnsZero()
    {
        // Arrange

        // Act
        var result = _service.CalculatePercentile([], new PercentileValue(0.75));

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Fact(DisplayName = "CalculatePercentile returns single value when list contains one item")]
    [Trait("Category", "Unit")]
    public void CalculatePercentileWhenValuesContainOneItemReturnsThatItem()
    {
        // Arrange
        var values = new List<TimeSpan> { TimeSpan.FromHours(4) };

        // Act
        var result = _service.CalculatePercentile(values, new PercentileValue(0.75));

        // Assert
        result.Should().Be(TimeSpan.FromHours(4));
    }

    [Fact(DisplayName = "CalculatePercentile interpolates expected value for p75")]
    [Trait("Category", "Unit")]
    public void CalculatePercentileWhenP75IsRequestedInterpolatesExpectedValue()
    {
        // Arrange
        var values = new List<TimeSpan>
        {
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(3),
            TimeSpan.FromHours(5)
        };

        // Act
        var result = _service.CalculatePercentile(values, new PercentileValue(0.75));

        // Assert
        result.Should().Be(TimeSpan.FromHours(3.5));
    }
    private readonly JiraAnalyticsService _service = new();

}
