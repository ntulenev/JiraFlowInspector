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
            ResolutionDate = "2026-02-02T10:00:00Z",
            IssueType = new JiraIssueTypeResponse
            {
                Name = "Bug"
            },
            IssueLinks =
            [
                new JiraIssueLinkResponse
                {
                    Type = new JiraIssueLinkTypeResponse
                    {
                        Inward = "is caused by",
                        Outward = "causes"
                    },
                    InwardIssue = new JiraIssueLinkIssueResponse
                    {
                        Key = "ADF-1"
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"summary\":\"Summary\"");
        json.Should().Contain("\"created\":\"2026-02-01T10:00:00Z\"");
        json.Should().Contain("\"resolutiondate\":\"2026-02-02T10:00:00Z\"");
        json.Should().Contain("\"issuetype\":{\"name\":\"Bug\"}");
        json.Should().Contain("\"issuelinks\":[");
        json.Should().Contain("\"inward\":\"is caused by\"");
        json.Should().Contain("\"outward\":\"causes\"");
        json.Should().Contain("\"inwardIssue\":{\"key\":\"ADF-1\"}");
    }
}
