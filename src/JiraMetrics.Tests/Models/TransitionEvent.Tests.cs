using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class TransitionEventTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var from = new StatusName("Open");
        var to = new StatusName("Done");
        var at = DateTimeOffset.UtcNow;
        var sincePrevious = TimeSpan.FromHours(2);

        // Act
        var transition = new TransitionEvent(from, to, at, sincePrevious);

        // Assert
        transition.From.Should().Be(from);
        transition.To.Should().Be(to);
        transition.At.Should().Be(at);
        transition.SincePrevious.Should().Be(sincePrevious);
    }
}
