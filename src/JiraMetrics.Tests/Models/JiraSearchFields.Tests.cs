using System.Collections;

using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraSearchFieldsTests
{
    [Fact(DisplayName = "Constructor throws when values are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesAreNullThrowsArgumentNullException()
    {
        IEnumerable<string> values = null!;

        Action act = () => _ = new JiraSearchFields(values);

        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Empty returns empty field selection")]
    [Trait("Category", "Unit")]
    public void EmptyWhenRequestedReturnsEmptySelection()
    {
        var fields = JiraSearchFields.Empty;

        fields.Count.Should().Be(0);
        fields.ToList().Should().BeEmpty();
    }

    [Fact(DisplayName = "From normalizes, trims, and deduplicates field names")]
    [Trait("Category", "Unit")]
    public void FromWhenValuesContainNoiseBuildsNormalizedFieldSelection()
    {
        string empty = null!;

        var fields = JiraSearchFields.From("  summary  ", "SUMMARY", " assignee ", "   ", empty);

        fields.Count.Should().Be(2);
        fields[0].Should().Be("summary");
        fields[1].Should().Be("assignee");
        fields.ToList().Should().Equal("summary", "assignee");
    }

    [Fact(DisplayName = "Enumeration returns normalized field values")]
    [Trait("Category", "Unit")]
    public void GetEnumeratorWhenCalledReturnsNormalizedValues()
    {
        var fields = JiraSearchFields.From("summary", "assignee");

        var genericValues = fields.ToArray();
        var nonGenericEnumerator = ((IEnumerable)fields).GetEnumerator();
        var nonGenericValues = new List<string>();
        while (nonGenericEnumerator.MoveNext())
        {
            nonGenericValues.Add((string)nonGenericEnumerator.Current!);
        }

        genericValues.Should().Equal("summary", "assignee");
        nonGenericValues.Should().Equal("summary", "assignee");
    }
}
