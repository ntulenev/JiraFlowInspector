using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTypeCountSummaryTests
{
    [Fact(DisplayName = "Constructor stores issue type and count")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledStoresValues()
    {
        // Arrange
        var issueType = new IssueTypeName("UserStory");
        var count = new ItemCount(7);

        // Act
        var summary = new IssueTypeCountSummary(issueType, count);

        // Assert
        summary.IssueType.Should().Be(issueType);
        summary.Count.Should().Be(count);
    }
}
