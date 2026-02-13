using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class LoadFailureTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var issueKey = new IssueKey("AAA-1");
        var reason = new ErrorMessage("Boom");

        // Act
        var failure = new LoadFailure(issueKey, reason);

        // Assert
        failure.IssueKey.Should().Be(issueKey);
        failure.Reason.Should().Be(reason);
    }
}

