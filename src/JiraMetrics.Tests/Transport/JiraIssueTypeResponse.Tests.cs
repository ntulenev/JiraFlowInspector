using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraIssueTypeResponseTests
{
    [Fact(DisplayName = "JiraIssueTypeResponse serializes expected json property")]
    [Trait("Category", "Unit")]
    public void SerializeWhenNameIsSetUsesExpectedJsonProperty()
    {
        // Arrange
        var dto = new JiraIssueTypeResponse
        {
            Name = "Task"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"name\":\"Task\"");
    }
}
