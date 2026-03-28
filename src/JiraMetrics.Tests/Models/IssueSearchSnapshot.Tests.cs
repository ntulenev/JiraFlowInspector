using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueSearchSnapshotTests
{
    [Fact(DisplayName = "BuildRatioSnapshot derives open and finished counters from search results")]
    [Trait("Category", "Unit")]
    public void BuildRatioSnapshotWhenCalledCalculatesCountersAndOpenIssues()
    {
        // Arrange
        var createdIssues = new List<IssueListItem>
        {
            new(new IssueKey("AAA-1"), new IssueSummary("Open issue")),
            new(new IssueKey("AAA-2"), new IssueSummary("Done issue")),
            new(new IssueKey("AAA-3"), new IssueSummary("Rejected issue"))
        };
        var snapshot = new IssueSearchSnapshot(
            createdIssues,
            [new IssueListItem(new IssueKey("AAA-2"), new IssueSummary("Done issue"))],
            [new IssueListItem(new IssueKey("AAA-3"), new IssueSummary("Rejected issue"))]);

        // Act
        var ratio = snapshot.BuildRatioSnapshot();

        // Assert
        ratio.CreatedThisMonth.Value.Should().Be(3);
        ratio.OpenThisMonth.Value.Should().Be(1);
        ratio.MovedToDoneThisMonth.Value.Should().Be(1);
        ratio.RejectedThisMonth.Value.Should().Be(1);
        ratio.FinishedThisMonth.Value.Should().Be(2);
        ratio.OpenIssues.Should().ContainSingle();
        ratio.OpenIssues[0].Key.Value.Should().Be("AAA-1");
    }
}
