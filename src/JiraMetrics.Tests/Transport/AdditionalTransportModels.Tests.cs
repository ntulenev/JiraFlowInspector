using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class AdditionalTransportModelsTests
{
    [Fact(DisplayName = "JiraBulkChangelogFetchRequest serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void JiraBulkChangelogFetchRequestWhenSerializedUsesExpectedJsonProperties()
    {
        var dto = new JiraBulkChangelogFetchRequest
        {
            FieldIds = ["customfield_12345"],
            IssueIdsOrKeys = ["AAA-1"],
            MaxResults = 100,
            NextPageToken = "token-1"
        };

        var json = JsonSerializer.Serialize(dto);

        json.Should().Contain("\"fieldIds\":[\"customfield_12345\"]");
        json.Should().Contain("\"issueIdsOrKeys\":[\"AAA-1\"]");
        json.Should().Contain("\"maxResults\":100");
        json.Should().Contain("\"nextPageToken\":\"token-1\"");
    }

    [Fact(DisplayName = "Bulk transport responses serialize expected json properties")]
    [Trait("Category", "Unit")]
    public void BulkTransportResponsesWhenSerializedUseExpectedJsonProperties()
    {
        var changelog = new JiraBulkIssueChangelogResponse
        {
            IssueId = "10001",
            ChangeHistories = []
        };
        var changelogResponse = new JiraBulkChangelogFetchResponse
        {
            IssueChangeLogs = [changelog],
            NextPageToken = "token-2"
        };
        var issueFetchRequest = new JiraBulkIssueFetchRequest
        {
            Fields = ["summary"],
            FieldsByKeys = true,
            IssueIdsOrKeys = ["AAA-1"],
            Properties = ["status"]
        };
        var issueFetchResponse = new JiraBulkIssueFetchResponse
        {
            Issues = [new JiraIssueResponse { Key = "AAA-1" }]
        };

        var changelogJson = JsonSerializer.Serialize(changelog);
        var changelogResponseJson = JsonSerializer.Serialize(changelogResponse);
        var issueFetchRequestJson = JsonSerializer.Serialize(issueFetchRequest);
        var issueFetchResponseJson = JsonSerializer.Serialize(issueFetchResponse);

        changelogJson.Should().Contain("\"issueId\":\"10001\"");
        changelogJson.Should().Contain("\"changeHistories\"");

        changelogResponseJson.Should().Contain("\"issueChangeLogs\"");
        changelogResponseJson.Should().Contain("\"nextPageToken\":\"token-2\"");

        issueFetchRequestJson.Should().Contain("\"fields\":[\"summary\"]");
        issueFetchRequestJson.Should().Contain("\"fieldsByKeys\":true");
        issueFetchRequestJson.Should().Contain("\"issueIdsOrKeys\":[\"AAA-1\"]");
        issueFetchRequestJson.Should().Contain("\"properties\":[\"status\"]");

        issueFetchResponseJson.Should().Contain("\"issues\"");
        issueFetchResponseJson.Should().Contain("\"key\":\"AAA-1\"");
    }

    [Fact(DisplayName = "JiraFieldResponse serializes expected json properties")]
    [Trait("Category", "Unit")]
    public void JiraFieldResponseWhenSerializedUsesExpectedJsonProperties()
    {
        var dto = new JiraFieldResponse
        {
            Id = "customfield_12345",
            Name = "Release Date"
        };

        var json = JsonSerializer.Serialize(dto);

        json.Should().Contain("\"id\":\"customfield_12345\"");
        json.Should().Contain("\"name\":\"Release Date\"");
    }

    [Fact(DisplayName = "Issue-link transport models serialize expected json properties")]
    [Trait("Category", "Unit")]
    public void IssueLinkTransportModelsWhenSerializedUseExpectedJsonProperties()
    {
        var linkIssue = new JiraIssueLinkIssueResponse { Key = "AAA-2" };
        var linkType = new JiraIssueLinkTypeResponse
        {
            Inward = "is blocked by",
            Outward = "blocks"
        };
        var link = new JiraIssueLinkResponse
        {
            Type = linkType,
            InwardIssue = linkIssue,
            OutwardIssue = new JiraIssueLinkIssueResponse { Key = "AAA-3" }
        };

        var linkIssueJson = JsonSerializer.Serialize(linkIssue);
        var linkTypeJson = JsonSerializer.Serialize(linkType);
        var linkJson = JsonSerializer.Serialize(link);

        linkIssueJson.Should().Contain("\"key\":\"AAA-2\"");
        linkTypeJson.Should().Contain("\"inward\":\"is blocked by\"");
        linkTypeJson.Should().Contain("\"outward\":\"blocks\"");
        linkJson.Should().Contain("\"type\"");
        linkJson.Should().Contain("\"inwardIssue\"");
        linkJson.Should().Contain("\"outwardIssue\"");
    }

    [Fact(DisplayName = "Status and subtask transport models serialize expected json properties")]
    [Trait("Category", "Unit")]
    public void StatusAndSubtaskTransportModelsWhenSerializedUseExpectedJsonProperties()
    {
        var status = new JiraIssueStatusResponse { Name = "Done" };
        var subtask = new JiraSubtaskResponse { Key = "AAA-4" };

        var statusJson = JsonSerializer.Serialize(status);
        var subtaskJson = JsonSerializer.Serialize(subtask);

        statusJson.Should().Contain("\"name\":\"Done\"");
        subtaskJson.Should().Contain("\"key\":\"AAA-4\"");
    }
}
