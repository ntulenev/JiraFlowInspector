using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraAnalyticsServiceTests
{
    private readonly JiraAnalyticsService _service = new();

    [Fact(DisplayName = "BuildPathKey returns no transitions key when list is empty")]
    [Trait("Category", "Unit")]
    public void BuildPathKeyWhenTransitionsAreEmptyReturnsNoTransitionsKey()
    {
        // Arrange
        var transitions = Array.Empty<TransitionEvent>();

        // Act
        var pathKey = _service.BuildPathKey(transitions);

        // Assert
        pathKey.Value.Should().Be("__NO_TRANSITIONS__");
    }

    [Fact(DisplayName = "BuildPathKey builds uppercase combined key")]
    [Trait("Category", "Unit")]
    public void BuildPathKeyWhenTransitionsExistReturnsCombinedKey()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("open"), new StatusName("code review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
            new(new StatusName("code review"), new StatusName("done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
        };

        // Act
        var pathKey = _service.BuildPathKey(transitions);

        // Assert
        pathKey.Value.Should().Be("OPEN->CODE REVIEW||CODE REVIEW->DONE");
    }

    [Fact(DisplayName = "BuildPathLabel returns no transitions label when list is empty")]
    [Trait("Category", "Unit")]
    public void BuildPathLabelWhenTransitionsAreEmptyReturnsNoTransitionsLabel()
    {
        // Arrange
        var transitions = Array.Empty<TransitionEvent>();

        // Act
        var pathLabel = _service.BuildPathLabel(transitions);

        // Assert
        pathLabel.Value.Should().Be("No transitions");
    }

    [Fact(DisplayName = "PathContainsStage returns true when stage matches from or to case-insensitively")]
    [Trait("Category", "Unit")]
    public void PathContainsStageWhenStageMatchesFromOrToReturnsTrue()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
        };

        // Act
        var result = _service.PathContainsStage(transitions, new StageName("code review"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "PathContainsStage returns false when stage does not exist")]
    [Trait("Category", "Unit")]
    public void PathContainsStageWhenStageDoesNotExistReturnsFalse()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("In Progress"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1))
        };

        // Act
        var result = _service.PathContainsStage(transitions, new StageName("Blocked"));

        // Assert
        result.Should().BeFalse();
    }

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

    [Fact(DisplayName = "Truncate returns same summary when length fits")]
    [Trait("Category", "Unit")]
    public void TruncateWhenSummaryFitsReturnsSameSummary()
    {
        // Arrange
        var summary = new IssueSummary("Short");

        // Act
        var truncated = _service.Truncate(summary, new TextLength(10));

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
        var truncated = _service.Truncate(summary, new TextLength(10));

        // Assert
        truncated.Value.Should().Be("This is...");
    }

    [Fact(DisplayName = "FormatDuration returns expected text for mixed duration")]
    [Trait("Category", "Unit")]
    public void FormatDurationWhenDurationHasDaysHoursAndMinutesReturnsExpectedText()
    {
        // Arrange
        var duration = new TimeSpan(days: 1, hours: 2, minutes: 30, seconds: 45);

        // Act
        var result = _service.FormatDuration(duration);

        // Assert
        result.Value.Should().Be("1d 2h 30m");
    }

    [Fact(DisplayName = "FormatDuration returns seconds when no larger units exist")]
    [Trait("Category", "Unit")]
    public void FormatDurationWhenOnlySecondsExistReturnsSecondsText()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(9);

        // Act
        var result = _service.FormatDuration(duration);

        // Assert
        result.Value.Should().Be("9s");
    }

    [Fact(DisplayName = "FormatDuration normalizes negative duration to zero")]
    [Trait("Category", "Unit")]
    public void FormatDurationWhenDurationIsNegativeReturnsZeroSeconds()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(-5);

        // Act
        var result = _service.FormatDuration(duration);

        // Assert
        result.Value.Should().Be("0s");
    }
}