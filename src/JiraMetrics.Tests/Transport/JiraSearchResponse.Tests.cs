using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraSearchResponseTests
{
    [Fact(DisplayName = "JiraSearchResponse initializes issues with empty list")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCalledInitializesIssuesWithEmptyList()
    {
        // Arrange

        // Act
        var dto = new JiraSearchResponse();

        // Assert
        dto.Issues.Should().NotBeNull();
        dto.Issues.Should().BeEmpty();
    }

    [Fact(DisplayName = "JiraSearchResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-1" }],
            IsLast = true,
            NextPageToken = "token"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"issues\"");
        json.Should().Contain("\"key\":\"AAA-1\"");
        json.Should().Contain("\"isLast\":true");
        json.Should().Contain("\"nextPageToken\":\"token\"");
    }
}

