using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API.Mapping;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.API.Mapping;

public sealed class RoadmapMappingTests
{
    [Theory(DisplayName = "Interval field parser accepts supported Jira references")]
    [Trait("Category", "Unit")]
    [InlineData("cf[12345][startDate]", "customfield_12345", "start")]
    [InlineData(" CF[987][ENDDATE] ", "customfield_987", "end")]
    public void TryParseIntervalFieldWhenReferenceIsSupportedReturnsField(
        string configuredField,
        string expectedFieldId,
        string expectedPropertyName)
    {
        // Act
        var parsed = RoadmapFieldReferenceParser.TryParseIntervalField(configuredField, out var field);

        // Assert
        parsed.Should().BeTrue();
        field.FieldId.Value.Should().Be(expectedFieldId);
        field.JsonPropertyName.Should().Be(expectedPropertyName);
    }

    [Theory(DisplayName = "Interval field parser rejects unsupported Jira references")]
    [Trait("Category", "Unit")]
    [InlineData("customfield_12345")]
    [InlineData("cf[12345]")]
    [InlineData("cf[][startDate]")]
    [InlineData("cf[abc][startDate]")]
    [InlineData("cf[12345][duration]")]
    [InlineData("cf[12345][startDate")]
    public void TryParseIntervalFieldWhenReferenceIsUnsupportedReturnsFalse(string configuredField)
    {
        // Act
        var parsed = RoadmapFieldReferenceParser.TryParseIntervalField(configuredField, out var field);

        // Assert
        parsed.Should().BeFalse();
        field.FieldId.Value.Should().BeNull();
        field.JsonPropertyName.Should().BeNull();
    }

    [Fact(DisplayName = "Roadmap mapper requests all configured fields")]
    [Trait("Category", "Unit")]
    public void BuildRequestedFieldsReturnsBaseAndConfiguredFields()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var context = CreateContext();

        // Act
        var fields = mapper.BuildRequestedFields(context);

        // Assert
        fields.Should().Equal(
            "key",
            "summary",
            "status",
            "customfield_10001",
            "customfield_10002",
            "customfield_10003");
    }

    [Fact(DisplayName = "Roadmap mapper reads dropdown and interval date variants")]
    [Trait("Category", "Unit")]
    public void MapIssuesWhenPayloadContainsSupportedVariantsReturnsRoadmapItem()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var issue = new JiraIssueKeyResponse
        {
            Key = "FLOW-42",
            Fields = new JiraIssueFieldsResponse
            {
                Summary = " Delivery milestone ",
                Status = new JiraIssueStatusResponse { Name = " In progress " },
                AdditionalFields = new Dictionary<string, JsonElement>
                {
                    ["customfield_10001"] = Parse("{\"value\":\" Platform \"}"),
                    ["customfield_10002"] = Parse("{\"start\":\"2026-07-01\"}"),
                    ["customfield_10003"] = Parse("\"{\\\"end\\\":\\\"2026-07-31T21:00:00+02:00\\\"}\"")
                }
            }
        };

        // Act
        var result = mapper.MapIssues([issue], CreateContext());

        // Assert
        result.Should().ContainSingle();
        result[0].Key.Value.Should().Be("FLOW-42");
        result[0].Summary.Value.Should().Be("Delivery milestone");
        result[0].Status.Should().Be("In progress");
        result[0].Roadmap.Should().Be("Platform");
        result[0].StartDate.Should().Be(new DateOnly(2026, 7, 1));
        result[0].EndDate.Should().Be(new DateOnly(2026, 7, 31));
    }

    [Fact(DisplayName = "Roadmap mapper uses safe fallbacks for missing optional fields")]
    [Trait("Category", "Unit")]
    public void MapIssuesWhenOptionalFieldsAreMissingReturnsFallbackValues()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var issue = new JiraIssueKeyResponse
        {
            Key = "FLOW-43",
            Fields = new JiraIssueFieldsResponse()
        };

        // Act
        var result = mapper.MapIssues([issue], CreateContext());

        // Assert
        result.Should().ContainSingle();
        result[0].Summary.Value.Should().Be("FLOW-43");
        result[0].Status.Should().Be("-");
        result[0].Roadmap.Should().BeNull();
        result[0].StartDate.Should().BeNull();
        result[0].EndDate.Should().BeNull();
    }

    [Fact(DisplayName = "Roadmap mapper ignores malformed optional field payloads")]
    [Trait("Category", "Unit")]
    public void MapIssuesWhenOptionalFieldsAreMalformedReturnsNullOptionalValues()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var issue = new JiraIssueKeyResponse
        {
            Key = "FLOW-44",
            Fields = new JiraIssueFieldsResponse
            {
                AdditionalFields = new Dictionary<string, JsonElement>
                {
                    ["customfield_10001"] = Parse("{\"value\":42}"),
                    ["customfield_10002"] = Parse("\"not-json\""),
                    ["customfield_10003"] = Parse("{\"end\":false}")
                }
            }
        };

        // Act
        var act = () => mapper.MapIssues([issue], CreateContext());

        // Assert
        act.Should().NotThrow();
        var result = act();
        result[0].Roadmap.Should().BeNull();
        result[0].StartDate.Should().BeNull();
        result[0].EndDate.Should().BeNull();
    }

    [Fact(DisplayName = "Roadmap mapper rejects issue without key")]
    [Trait("Category", "Unit")]
    public void MapIssuesWhenIssueKeyIsMissingThrowsInvalidOperationException()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var issue = new JiraIssueKeyResponse { Fields = new JiraIssueFieldsResponse() };

        // Act
        Action act = () => _ = mapper.MapIssues([issue], CreateContext());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Roadmap issue key is missing.");
    }

    [Fact(DisplayName = "Roadmap mapper rejects issue without fields")]
    [Trait("Category", "Unit")]
    public void MapIssuesWhenIssueFieldsAreMissingThrowsInvalidOperationException()
    {
        // Arrange
        var mapper = new RoadmapItemMapper();
        var issue = new JiraIssueKeyResponse { Key = "FLOW-45" };

        // Act
        Action act = () => _ = mapper.MapIssues([issue], CreateContext());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Roadmap issue 'FLOW-45' fields are missing.");
    }

    private static RoadmapMappingContext CreateContext() =>
        new(
            new JiraFieldId("customfield_10001"),
            new RoadmapDateFieldReference(new JiraFieldId("customfield_10002"), "start"),
            new RoadmapDateFieldReference(new JiraFieldId("customfield_10003"), "end"));

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
