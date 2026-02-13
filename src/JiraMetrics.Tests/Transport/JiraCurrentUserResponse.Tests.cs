using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraCurrentUserResponseTests
{
    [Fact(DisplayName = "JiraCurrentUserResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraCurrentUserResponse
        {
            DisplayName = "Jane Doe",
            EmailAddress = "user@example.com",
            AccountId = "123"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"displayName\":\"Jane Doe\"");
        json.Should().Contain("\"emailAddress\":\"user@example.com\"");
        json.Should().Contain("\"accountId\":\"123\"");
    }
}
