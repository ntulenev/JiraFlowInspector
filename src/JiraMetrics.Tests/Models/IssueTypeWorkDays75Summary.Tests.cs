using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTypeWorkDays75SummaryTests
{
    [Fact(DisplayName = "Constructor stores issue type count and p75 duration")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledStoresValues()
    {
        // Arrange
        var issueType = new IssueTypeName("Task");
        var issueCount = new ItemCount(5);
        var duration = TimeSpan.FromDays(2.5);

        // Act
        var summary = new IssueTypeWorkDays75Summary(issueType, issueCount, duration);

        // Assert
        summary.IssueType.Should().Be(issueType);
        summary.IssueCount.Should().Be(issueCount);
        summary.DaysAtWorkP75.Should().Be(duration);
    }

    [Fact(DisplayName = "Constructor clamps negative p75 duration to zero")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDurationIsNegativeClampsToZero()
    {
        // Act
        var summary = new IssueTypeWorkDays75Summary(
            new IssueTypeName("Task"),
            new ItemCount(1),
            TimeSpan.FromDays(-1));

        // Assert
        summary.DaysAtWorkP75.Should().Be(TimeSpan.Zero);
    }
}
