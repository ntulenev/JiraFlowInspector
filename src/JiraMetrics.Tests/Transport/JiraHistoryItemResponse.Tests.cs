using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraHistoryItemResponseTests
{
    [Fact(DisplayName = "JiraHistoryItemResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraHistoryItemResponse
        {
            Field = "status",
            FromStatus = "Open",
            ToStatus = "Done"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"field\":\"status\"");
        json.Should().Contain("\"fromString\":\"Open\"");
        json.Should().Contain("\"toString\":\"Done\"");
    }
}
