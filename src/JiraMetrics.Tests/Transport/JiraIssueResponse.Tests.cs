using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraIssueResponseTests
{
    [Fact(DisplayName = "JiraIssueResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraIssueResponse
        {
            Key = "AAA-1",
            Fields = new JiraIssueFieldsResponse
            {
                Summary = "Summary",
                Created = "2026-02-01T10:00:00Z"
            },
            Changelog = new JiraChangelogResponse
            {
                Histories = [new JiraHistoryResponse()]
            }
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"key\":\"AAA-1\"");
        json.Should().Contain("\"fields\"");
        json.Should().Contain("\"changelog\"");
    }
}

