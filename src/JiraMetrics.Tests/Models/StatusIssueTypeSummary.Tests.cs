using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class StatusIssueTypeSummaryTests
{
    [Fact(DisplayName = "Constructor throws when issue types are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenIssueTypesAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<IssueTypeCountSummary> issueTypes = null!;

        // Act
        Action act = () => _ = new StatusIssueTypeSummary(
            new StatusName("QA"),
            new ItemCount(1),
            issueTypes);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor stores status count and copies issue type summaries")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledStoresValues()
    {
        // Arrange
        var issueTypes = new List<IssueTypeCountSummary>
        {
            new(new IssueTypeName("UserStory"), new ItemCount(2))
        };

        // Act
        var summary = new StatusIssueTypeSummary(
            new StatusName("QA"),
            new ItemCount(2),
            issueTypes);

        // Assert
        summary.Status.Should().Be(new StatusName("QA"));
        summary.Count.Should().Be(new ItemCount(2));
        summary.IssueTypes.Should().ContainSingle();

        issueTypes.Add(new IssueTypeCountSummary(new IssueTypeName("SubTask"), new ItemCount(1)));
        summary.IssueTypes.Should().ContainSingle();
    }
}
