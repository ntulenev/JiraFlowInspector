using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class PercentileTransitionTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var from = new StatusName("Open");
        var to = new StatusName("Done");
        var p75Duration = TimeSpan.FromHours(5);

        // Act
        var transition = new PercentileTransition(from, to, p75Duration);

        // Assert
        transition.From.Should().Be(from);
        transition.To.Should().Be(to);
        transition.P75Duration.Should().Be(p75Duration);
    }
}
