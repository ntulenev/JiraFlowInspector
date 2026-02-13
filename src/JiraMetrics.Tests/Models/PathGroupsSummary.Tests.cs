using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PathGroupsSummaryTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var successfulCount = new ItemCount(10);
        var matchedStageCount = new ItemCount(7);
        var failedCount = new ItemCount(2);
        var pathGroupCount = new ItemCount(3);

        // Act
        var summary = new PathGroupsSummary(successfulCount, matchedStageCount, failedCount, pathGroupCount);

        // Assert
        summary.SuccessfulCount.Should().Be(successfulCount);
        summary.MatchedStageCount.Should().Be(matchedStageCount);
        summary.FailedCount.Should().Be(failedCount);
        summary.PathGroupCount.Should().Be(pathGroupCount);
    }
}
