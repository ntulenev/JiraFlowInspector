using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraHistoryResponseTests
{
    [Fact(DisplayName = "JiraHistoryResponse initializes items with empty list")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledInitializesItemsWithEmptyList()
    {
        // Arrange

        // Act
        var dto = new JiraHistoryResponse();

        // Assert
        dto.Items.Should().NotBeNull();
        dto.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "JiraHistoryResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraHistoryResponse
        {
            Created = "2026-02-01T10:00:00Z",
            Items = [new JiraHistoryItemResponse { Field = "status" }]
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"created\":\"2026-02-01T10:00:00Z\"");
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"field\":\"status\"");
    }
}
