using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class HotFixRuleTests
{
    [Fact(DisplayName = "Constructor throws when values are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesAreNullThrowsArgumentNullException()
    {
        IReadOnlyList<JiraFieldValue> values = null!;

        Action act = () => _ = new HotFixRule(new JiraFieldName("Fix Type"), values);

        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when values are empty")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesAreEmptyThrowsArgumentException()
    {
        Action act = () => _ = new HotFixRule(new JiraFieldName("Fix Type"), []);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*at least one value*");
    }

    [Fact(DisplayName = "Constructor sorts and deduplicates hot-fix values")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesContainDuplicatesSortsAndDeduplicatesThem()
    {
        var values = new List<JiraFieldValue>
        {
            new("beta"),
            new("Alpha"),
            new("beta")
        };

        var rule = new HotFixRule(new JiraFieldName("Fix Type"), values);

        rule.FieldName.Should().Be(new JiraFieldName("Fix Type"));
        rule.Values.Select(static value => value.Value).Should().Equal("Alpha", "beta");
    }
}
