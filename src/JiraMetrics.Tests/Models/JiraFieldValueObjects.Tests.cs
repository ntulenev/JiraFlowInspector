using FluentAssertions;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraFieldIdTests
{
    [Fact(DisplayName = "Constructor throws when field id is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        string value = null!;

        Action act = () => _ = new JiraFieldId(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when field id is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        const string value = "   ";

        Action act = () => _ = new JiraFieldId(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims field id value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        var fieldId = new JiraFieldId("  customfield_12345  ");

        fieldId.Value.Should().Be("customfield_12345");
    }

    [Fact(DisplayName = "FromNullable returns null for empty field id")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsNullOrWhiteSpaceReturnsNull()
    {
        var fromNull = JiraFieldId.FromNullable(null);
        var fromWhitespace = JiraFieldId.FromNullable("   ");

        fromNull.Should().BeNull();
        fromWhitespace.Should().BeNull();
    }

    [Fact(DisplayName = "FromNullable returns trimmed field id")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsProvidedReturnsTrimmedValue()
    {
        var fieldId = JiraFieldId.FromNullable("  customfield_12345  ");

        fieldId.Should().Be(new JiraFieldId("customfield_12345"));
    }

    [Fact(DisplayName = "ToString returns field id value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        var fieldId = new JiraFieldId("customfield_12345");

        var text = fieldId.ToString();

        text.Should().Be("customfield_12345");
    }
}

public sealed class JiraFieldNameTests
{
    [Fact(DisplayName = "Constructor throws when field name is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        string value = null!;

        Action act = () => _ = new JiraFieldName(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when field name is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        const string value = "   ";

        Action act = () => _ = new JiraFieldName(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims field name value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        var fieldName = new JiraFieldName("  Release Date  ");

        fieldName.Value.Should().Be("Release Date");
    }

    [Fact(DisplayName = "FromNullable returns null for empty field name")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsNullOrWhiteSpaceReturnsNull()
    {
        var fromNull = JiraFieldName.FromNullable(null);
        var fromWhitespace = JiraFieldName.FromNullable("   ");

        fromNull.Should().BeNull();
        fromWhitespace.Should().BeNull();
    }

    [Fact(DisplayName = "FromNullable returns trimmed field name")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsProvidedReturnsTrimmedValue()
    {
        var fieldName = JiraFieldName.FromNullable("  Release Date  ");

        fieldName.Should().Be(new JiraFieldName("Release Date"));
    }

    [Fact(DisplayName = "ToString returns field name value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        var fieldName = new JiraFieldName("Release Date");

        var text = fieldName.ToString();

        text.Should().Be("Release Date");
    }
}

public sealed class JiraFieldValueTests
{
    [Fact(DisplayName = "Constructor throws when field value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsNullThrowsArgumentException()
    {
        string value = null!;

        Action act = () => _ = new JiraFieldValue(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when field value is whitespace")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueIsWhiteSpaceThrowsArgumentException()
    {
        const string value = "   ";

        Action act = () => _ = new JiraFieldValue(value);

        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor trims field value")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValueContainsPaddingTrimsValue()
    {
        var fieldValue = new JiraFieldValue("  Production  ");

        fieldValue.Value.Should().Be("Production");
    }

    [Fact(DisplayName = "FromNullable returns null for empty field value")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsNullOrWhiteSpaceReturnsNull()
    {
        var fromNull = JiraFieldValue.FromNullable(null);
        var fromWhitespace = JiraFieldValue.FromNullable("   ");

        fromNull.Should().BeNull();
        fromWhitespace.Should().BeNull();
    }

    [Fact(DisplayName = "FromNullable returns trimmed field value")]
    [Trait("Category", "Unit")]
    public void FromNullableWhenValueIsProvidedReturnsTrimmedValue()
    {
        var fieldValue = JiraFieldValue.FromNullable("  Production  ");

        fieldValue.Should().Be(new JiraFieldValue("Production"));
    }

    [Fact(DisplayName = "ToString returns field value")]
    [Trait("Category", "Unit")]
    public void ToStringWhenCalledReturnsValue()
    {
        var fieldValue = new JiraFieldValue("Production");

        var text = fieldValue.ToString();

        text.Should().Be("Production");
    }
}
