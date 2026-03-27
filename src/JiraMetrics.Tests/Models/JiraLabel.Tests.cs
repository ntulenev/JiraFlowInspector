using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraLabelTests
{
    [Fact(DisplayName = "Constructor throws when label is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        string value = null!;

        Action act = () => _ = new JiraLabel(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when label is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        const string value = "   ";

        Action act = () => _ = new JiraLabel(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims label")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        var label = new JiraLabel("  release  ");

        label.Value.Should().Be("release");
    }

    [Fact(DisplayName = "ToString returns label value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        var label = new JiraLabel("release");

        var text = label.ToString();

        text.Should().Be("release");
    }
}
