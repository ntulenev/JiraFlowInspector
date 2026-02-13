using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraIssueKeyResponseTests
{
    [Fact(DisplayName = "JiraIssueKeyResponse serializes expected json property")]
    [Trait("Category", "Unit")]
    public void SerializeWhenKeyIsSetUsesExpectedJsonProperty()
    {
        // Arrange
        var dto = new JiraIssueKeyResponse
        {
            Key = "AAA-1"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"key\":\"AAA-1\"");
    }
}

