using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraChangelogResponseTests
{
    [Fact(DisplayName = "JiraChangelogResponse initializes histories with empty list")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledInitializesHistoriesWithEmptyList()
    {
        // Arrange

        // Act
        var dto = new JiraChangelogResponse();

        // Assert
        dto.Histories.Should().NotBeNull();
        dto.Histories.Should().BeEmpty();
    }

    [Fact(DisplayName = "JiraChangelogResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraChangelogResponse
        {
            Histories = [new JiraHistoryResponse { Created = "2026-02-01T10:00:00Z" }]
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"histories\"");
        json.Should().Contain("\"created\":\"2026-02-01T10:00:00Z\"");
    }
}
