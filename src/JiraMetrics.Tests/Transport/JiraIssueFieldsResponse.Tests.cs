using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraIssueFieldsResponseTests
{
    [Fact(DisplayName = "JiraIssueFieldsResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void SerializeWhenValuesAreSetUsesExpectedJsonProperties()
    {
        // Arrange
        var dto = new JiraIssueFieldsResponse
        {
            Summary = "Summary",
            Created = "2026-02-01T10:00:00Z",
            ResolutionDate = "2026-02-02T10:00:00Z"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"summary\":\"Summary\"");
        json.Should().Contain("\"created\":\"2026-02-01T10:00:00Z\"");
        json.Should().Contain("\"resolutiondate\":\"2026-02-02T10:00:00Z\"");
    }
}
