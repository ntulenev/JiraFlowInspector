using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API.Mapping;

namespace JiraMetrics.Tests.API.Mapping;

public sealed class JiraFieldValueParsersTests
{
    [Theory(DisplayName = "Pull request detector handles Jira development field variants")]
    [Trait("Category", "Unit")]
    [InlineData("null", false)]
    [InlineData("\"\"", false)]
    [InlineData("\"repository count: 2\"", false)]
    [InlineData("\"pullrequest\"", true)]
    [InlineData("\"pullrequest={stateCount=0}\"", false)]
    [InlineData("\"pullrequest={stateCount=2}\"", true)]
    [InlineData("\"pullrequest={count: 0, stateCount: 1}\"", true)]
    [InlineData("\"pullrequest={count: 999999999999999999999999}\"", false)]
    public void HasPullRequestInRawValueWhenPayloadVariesReturnsExpectedResult(
        string json,
        bool expected)
    {
        // Arrange
        var value = Parse(json);

        // Act
        var result = PullRequestDetector.HasPullRequest(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "Additional field lookup prefers field id over field name")]
    [Trait("Category", "Unit")]
    public void TryGetAdditionalFieldValueWhenIdAndNameExistReturnsIdValue()
    {
        // Arrange
        var fields = new Dictionary<string, JsonElement>
        {
            ["customfield_10001"] = Parse("\"by-id\""),
            ["Environment"] = Parse("\"by-name\"")
        };

        // Act
        var found = JiraFieldValueParser.TryGetValue(
            fields,
            "customfield_10001",
            "Environment",
            out var value);

        // Assert
        found.Should().BeTrue();
        value.GetString().Should().Be("by-id");
    }

    [Fact(DisplayName = "Additional field lookup falls back to field name")]
    [Trait("Category", "Unit")]
    public void TryGetAdditionalFieldValueWhenIdIsMissingReturnsNameValue()
    {
        // Arrange
        var fields = new Dictionary<string, JsonElement>
        {
            ["Environment"] = Parse("\"by-name\"")
        };

        // Act
        var found = JiraFieldValueParser.TryGetValue(
            fields,
            "customfield_10001",
            "Environment",
            out var value);

        // Assert
        found.Should().BeTrue();
        value.GetString().Should().Be("by-name");
    }

    [Fact(DisplayName = "Additional field lookup returns false when references are absent")]
    [Trait("Category", "Unit")]
    public void TryGetAdditionalFieldValueWhenReferencesAreAbsentReturnsFalse()
    {
        // Arrange
        // Act
        var found = JiraFieldValueParser.TryGetValue([], " ", null, out var value);

        // Assert
        found.Should().BeFalse();
        value.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Fact(DisplayName = "Raw field parser normalizes supported Jira value shapes")]
    [Trait("Category", "Unit")]
    public void ParseRawFieldValuesWhenArrayContainsSupportedShapesReturnsSortedDistinctValues()
    {
        // Arrange
        var value = Parse(
            """
            [
              " Beta ",
              { "value": "Alpha", "name": "ignored", "id": "ignored" },
              { "name": "Gamma", "id": "ignored" },
              { "id": "Delta" },
              42,
              true,
              "alpha",
              null,
              {}
            ]
            """);

        // Act
        var result = JiraFieldValueParser.Parse(value);

        // Assert
        result.Should().Equal("42", "Alpha", "Beta", "Delta", "Gamma", "true");
    }

    [Fact(DisplayName = "Raw field parser extracts nested Atlassian document text")]
    [Trait("Category", "Unit")]
    public void ParseRawFieldValuesWhenValueIsAtlassianDocumentReturnsCombinedText()
    {
        // Arrange
        var value = Parse(
            """
            {
              "type": "doc",
              "version": 1,
              "content": [
                {
                  "type": "paragraph",
                  "content": [
                    { "type": "text", "text": " First " },
                    { "type": "hardBreak" },
                    { "type": "text", "text": "second" }
                  ]
                },
                { "type": "paragraph", "content": [{ "type": "text", "text": "third" }] }
              ]
            }
            """);

        // Act
        var result = JiraFieldValueParser.Parse(value);

        // Assert
        result.Should().Equal("First second third");
    }

    [Theory(DisplayName = "Raw field parser ignores unsupported or empty values")]
    [Trait("Category", "Unit")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"   \"")]
    [InlineData("{\"type\":\"doc\",\"content\":[]}")]
    [InlineData("{\"type\":\"paragraph\",\"content\":[{\"text\":\"ignored\"}]}")]
    public void ParseRawFieldValuesWhenValueIsUnsupportedReturnsEmpty(string json)
    {
        // Arrange
        // Act
        var result = JiraFieldValueParser.Parse(Parse(json));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Component parser normalizes array values")]
    [Trait("Category", "Unit")]
    public void ParseComponentValuesWhenValueIsArrayReturnsSortedDistinctValues()
    {
        // Arrange
        var value = Parse(
            """
            [
              " UI ",
              { "value": "API", "name": "ignored", "id": "ignored" },
              { "name": "Platform", "id": "ignored" },
              { "id": "Legacy" },
              "api",
              null,
              42,
              {}
            ]
            """);

        // Act
        var result = ComponentValueReader.Read(value);

        // Assert
        result.Should().Equal("API", "Legacy", "Platform", "UI");
    }

    [Fact(DisplayName = "Component parser splits comma-separated string")]
    [Trait("Category", "Unit")]
    public void ParseComponentValuesWhenValueIsStringReturnsSortedDistinctValues()
    {
        // Arrange
        // Act
        var result = ComponentValueReader.Read(Parse("\" UI, API, ui, , Platform \""));

        // Assert
        result.Should().Equal("API", "Platform", "UI");
    }

    [Theory(DisplayName = "Component parser reads supported object properties")]
    [Trait("Category", "Unit")]
    [InlineData("{\"value\":\" Value \",\"name\":\"Name\",\"id\":\"Id\"}", "Value")]
    [InlineData("{\"value\":\" \",\"name\":\" Name \",\"id\":\"Id\"}", "Name")]
    [InlineData("{\"name\":\" \",\"id\":\" Id \"}", "Id")]
    public void ParseComponentValuesWhenValueIsObjectUsesPropertyPriority(
        string json,
        string expected)
    {
        // Arrange
        // Act
        var result = ComponentValueReader.Read(Parse(json));

        // Assert
        result.Should().Equal(expected);
    }

    [Theory(DisplayName = "Component parser ignores unsupported values")]
    [Trait("Category", "Unit")]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("\"   \"")]
    public void ParseComponentValuesWhenValueIsUnsupportedReturnsEmpty(string json)
    {
        // Arrange
        // Act
        var result = ComponentValueReader.Read(Parse(json));

        // Assert
        result.Should().BeEmpty();
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
