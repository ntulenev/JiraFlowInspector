using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JqlQueryTests
{
    [Fact(DisplayName = "Constructor throws when query is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        string value = null!;

        Action act = () => _ = new JqlQuery(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when query is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        const string value = "   ";

        Action act = () => _ = new JqlQuery(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims query text")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        var query = new JqlQuery("  project = TEST  ");

        query.Value.Should().Be("project = TEST");
    }

    [Fact(DisplayName = "ToString returns query text")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        var query = new JqlQuery("project = TEST");

        var text = query.ToString();

        text.Should().Be("project = TEST");
    }
}
