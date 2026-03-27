using System.Globalization;
using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraBulkHistoryResponseTests
{
    [Fact(DisplayName = "ToHistoryResponse converts Unix seconds to ISO timestamp")]
    [Trait("Category", "Unit")]
    public void ToHistoryResponseWhenCreatedIsUnixSecondsConvertsToIsoString()
    {
        var dto = new JiraBulkHistoryResponse
        {
            Created = ParseJsonElement("1704067200"),
            Items = [new JiraHistoryItemResponse { Field = "status" }]
        };

        var history = dto.ToHistoryResponse();

        history.Created.Should().Be(
            DateTimeOffset.FromUnixTimeSeconds(1704067200).ToString("O", CultureInfo.InvariantCulture));
        history.Items.Should().Equal(dto.Items);
    }

    [Fact(DisplayName = "ToHistoryResponse converts Unix milliseconds to ISO timestamp")]
    [Trait("Category", "Unit")]
    public void ToHistoryResponseWhenCreatedIsUnixMillisecondsConvertsToIsoString()
    {
        var dto = new JiraBulkHistoryResponse
        {
            Created = ParseJsonElement("1704067200000")
        };

        var history = dto.ToHistoryResponse();

        history.Created.Should().Be(
            DateTimeOffset.FromUnixTimeMilliseconds(1704067200000).ToString("O", CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "ToHistoryResponse keeps string timestamp as is")]
    [Trait("Category", "Unit")]
    public void ToHistoryResponseWhenCreatedIsStringReturnsStringValue()
    {
        var dto = new JiraBulkHistoryResponse
        {
            Created = ParseJsonElement("\"2026-03-01T08:00:00.0000000+00:00\"")
        };

        var history = dto.ToHistoryResponse();

        history.Created.Should().Be("2026-03-01T08:00:00.0000000+00:00");
    }

    [Fact(DisplayName = "ToHistoryResponse returns null for unsupported created value kinds")]
    [Trait("Category", "Unit")]
    public void ToHistoryResponseWhenCreatedHasUnsupportedKindReturnsNull()
    {
        var values = new[]
        {
            "123.45",
            "null",
            "true",
            "false",
            "{}",
            "[]"
        };

        var results = values
            .Select(static value => new JiraBulkHistoryResponse
            {
                Created = ParseJsonElement(value)
            }.ToHistoryResponse())
            .ToArray();

        results.Should().OnlyContain(static result => result.Created == null);
    }

    [Fact(DisplayName = "ToHistoryResponse returns null when Unix timestamp is out of range")]
    [Trait("Category", "Unit")]
    public void ToHistoryResponseWhenUnixTimestampIsOutOfRangeReturnsNull()
    {
        var dto = new JiraBulkHistoryResponse
        {
            Created = ParseJsonElement("9223372036854775807")
        };

        var history = dto.ToHistoryResponse();

        history.Created.Should().BeNull();
    }

    private static JsonElement ParseJsonElement(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
