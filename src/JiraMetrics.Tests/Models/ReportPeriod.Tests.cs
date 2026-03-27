using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class ReportPeriodTests
{
    [Fact(DisplayName = "FromMonthLabel uses month bounds and keeps month label")]
    [Trait("Category", "Unit")]
    public void FromMonthLabelWhenCalledBuildsMonthBasedPeriod()
    {
        // Arrange
        var monthLabel = new MonthLabel("2026-03");

        // Act
        var period = ReportPeriod.FromMonthLabel(monthLabel);

        // Assert
        period.IsMonthBased.Should().BeTrue();
        period.MonthLabel.Should().Be(monthLabel);
        period.Start.Should().Be(new DateOnly(2026, 3, 1));
        period.EndInclusive.Should().Be(new DateOnly(2026, 3, 31));
        period.EndExclusive.Should().Be(new DateOnly(2026, 4, 1));
        period.Label.Should().Be("2026-03");
    }

    [Fact(DisplayName = "FromDateRange uses inclusive end date and range label")]
    [Trait("Category", "Unit")]
    public void FromDateRangeWhenCalledBuildsExplicitRangePeriod()
    {
        // Act
        var period = ReportPeriod.FromDateRange(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 29));

        // Assert
        period.IsMonthBased.Should().BeFalse();
        period.MonthLabel.Should().BeNull();
        period.Start.Should().Be(new DateOnly(2026, 3, 16));
        period.EndInclusive.Should().Be(new DateOnly(2026, 3, 29));
        period.EndExclusive.Should().Be(new DateOnly(2026, 3, 30));
        period.Label.Should().Be("16.03.2026 - 29.03.2026");
    }

    [Theory(DisplayName = "TryParseConfiguredDate supports both accepted formats")]
    [InlineData("16.03.2026")]
    [InlineData("2026-03-16")]
    [Trait("Category", "Unit")]
    public void TryParseConfiguredDateWhenFormatIsSupportedReturnsTrue(string value)
    {
        // Act
        var parsed = ReportPeriod.TryParseConfiguredDate(value, out var date);

        // Assert
        parsed.Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 3, 16));
    }
}
