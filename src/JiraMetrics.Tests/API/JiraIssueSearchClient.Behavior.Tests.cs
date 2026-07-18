using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Search;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraIssueSearchClientBehaviorTests
{
    [Fact(DisplayName = "GetIssueKeysMovedToDoneThisMonthAsync returns distinct sorted keys")]
    [Trait("Category", "Unit")]
    public async Task GetIssueKeysMovedToDoneThisMonthAsyncWhenMultiplePagesReturnsDistinctSortedKeys()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;

        var firstResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-2" }, new JiraIssueKeyResponse { Key = "aaa-1" }],
            IsLast = false,
            NextPageToken = "next-1"
        };

        var secondResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-2" }, new JiraIssueKeyResponse { Key = "AAA-3" }],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/search/jql?", StringComparison.Ordinal)),
                cts.Token))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.Is<Uri>(u => u.ToString().Contains("nextPageToken=next-1", StringComparison.Ordinal)),
                cts.Token))
            .Callback(() => sendCalls++)
            .ReturnsAsync(secondResponse);

        var client = CreateClient(transport.Object);

        // Act
        var keys = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        keys.Select(key => key.Value).Should().ContainInOrder("aaa-1", "AAA-2", "AAA-3");
    }

    [Fact(DisplayName = "GetIssueKeysMovedToDoneThisMonthAsync adds created filter when created-after date is provided")]
    [Trait("Category", "Unit")]
    public async Task GetIssueKeysMovedToDoneThisMonthAsyncWhenCreatedAfterIsProvidedAddsCreatedClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-1" }],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var client = CreateClient(transport.Object);

        // Act
        _ = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new CreatedAfterDate("2026-01-15"),
            cts.Token);

        // Assert
        capturedUrl.Should().Contain("created");
        capturedUrl.Should().Contain("2026-01-15");
    }

    [Fact(DisplayName = "GetIssueKeysMovedToDoneThisMonthAsync adds custom field filter when configured")]
    [Trait("Category", "Unit")]
    public async Task GetIssueKeysMovedToDoneThisMonthAsyncWhenCustomFieldFilterIsConfiguredAddsClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-1" }],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(customFieldName: "Team", customFieldValue: "Import");
        var client = CreateClient(transport.Object, settings);

        // Act
        _ = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            cts.Token);

        // Assert
        capturedUrl.Should().Contain("Team");
        capturedUrl.Should().Contain("Import");
    }

    [Fact(DisplayName = "GetIssueKeysMovedToDoneThisMonthAsync uses configured month label in JQL")]
    [Trait("Category", "Unit")]
    public async Task GetIssueKeysMovedToDoneThisMonthAsyncWhenMonthLabelIsConfiguredUsesMonthRange()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-1" }],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-01");
        var client = CreateClient(transport.Object, settings);

        // Act
        _ = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            cts.Token);

        // Assert
        capturedUrl.Should().Contain("2026-01-01");
        capturedUrl.Should().Contain("2026-02-01");
    }

    [Fact(DisplayName = "GetIssueKeysMovedToDoneThisMonthAsync uses explicit from-to range in JQL")]
    [Trait("Category", "Unit")]
    public async Task GetIssueKeysMovedToDoneThisMonthAsyncWhenExplicitRangeIsConfiguredUsesRangeBounds()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues = [new JiraIssueKeyResponse { Key = "AAA-1" }],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(reportPeriod: ReportPeriod.FromDateRange(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 29)));
        var client = CreateClient(transport.Object, settings);

        // Act
        _ = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            cts.Token);

        // Assert
        capturedUrl.Should().Contain("2026-03-16");
        capturedUrl.Should().Contain("2026-03-30");
    }

    [Fact(DisplayName = "GetIssuesCreatedThisMonthAsync returns issue details with Jira id and title")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesCreatedThisMonthAsyncWhenCalledReturnsIssueDetails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "AAA-1",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Open bug",
                        Created = "2026-01-10T09:00:00Z",
                        Priority = new JiraPriorityResponse { Name = "P1" }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-01");
        var client = CreateClient(transport.Object, settings);

        // Act
        var issues = await client.GetIssuesCreatedThisMonthAsync(
            new ProjectKey("AAA"),
            [new IssueTypeName("Bug")],
            cts.Token);

        // Assert
        issues.Should().ContainSingle();
        issues[0].Key.Value.Should().Be("AAA-1");
        issues[0].Title.Value.Should().Be("Open bug");
        issues[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero));
        issues[0].Priority.Should().Be("P1");
        capturedUrl.Should().Contain("created");
        capturedUrl.Should().Contain("fields=key,summary,created,priority");
    }

    [Fact(DisplayName = "GetIssuesMovedToDoneThisMonthAsync returns issue details")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesMovedToDoneThisMonthAsyncWhenCalledReturnsIssueDetails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "AAA-2",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Done bug",
                        Created = "2026-01-11T10:00:00Z",
                        Priority = new JiraPriorityResponse { Name = "P2" }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var client = CreateClient(transport.Object);

        // Act
        var issues = await client.GetIssuesMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            [new IssueTypeName("Bug")],
            cts.Token);

        // Assert
        issues.Should().ContainSingle();
        issues[0].Key.Value.Should().Be("AAA-2");
        issues[0].Title.Value.Should().Be("Done bug");
        issues[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 1, 11, 10, 0, 0, TimeSpan.Zero));
        issues[0].Priority.Should().Be("P2");
        capturedUrl.Should().Contain("status");
        capturedUrl.Should().Contain("status%20%3D%20%22Done%22");
        capturedUrl.Should().Contain("fields=key,summary,created,priority");
    }

    [Fact(DisplayName = "GetIssueCountsByStatusExcludingDoneAndRejectAsync groups counts by status and type")]
    [Trait("Category", "Unit")]
    public async Task GetIssueCountsByStatusExcludingDoneAndRejectAsyncWhenCalledReturnsGroupedCounts()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "AAA-1",
                    Fields = new JiraIssueFieldsResponse
                    {
                        IssueType = new JiraIssueTypeResponse { Name = "UserStory" },
                        Status = new JiraIssueStatusResponse { Name = "QA" }
                    }
                },
                new JiraIssueKeyResponse
                {
                    Key = "AAA-2",
                    Fields = new JiraIssueFieldsResponse
                    {
                        IssueType = new JiraIssueTypeResponse { Name = "UserStory" },
                        Status = new JiraIssueStatusResponse { Name = "QA" }
                    }
                },
                new JiraIssueKeyResponse
                {
                    Key = "AAA-3",
                    Fields = new JiraIssueFieldsResponse
                    {
                        IssueType = new JiraIssueTypeResponse { Name = "SubTask" },
                        Status = new JiraIssueStatusResponse { Name = "QA" }
                    }
                },
                new JiraIssueKeyResponse
                {
                    Key = "AAA-4",
                    Fields = new JiraIssueFieldsResponse
                    {
                        IssueType = new JiraIssueTypeResponse { Name = "Task" },
                        Status = new JiraIssueStatusResponse { Name = "In Progress" }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(customFieldName: "Team", customFieldValue: "Import");
        var client = CreateClient(transport.Object, settings);

        // Act
        var result = await client.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            cts.Token);

        // Assert
        result.Should().HaveCount(2);
        result[0].Status.Value.Should().Be("QA");
        result[0].Count.Value.Should().Be(3);
        result[0].IssueTypes.Should().HaveCount(2);
        result[0].IssueTypes[0].IssueType.Value.Should().Be("UserStory");
        result[0].IssueTypes[0].Count.Value.Should().Be(2);
        result[0].IssueTypes[1].IssueType.Value.Should().Be("SubTask");
        result[0].IssueTypes[1].Count.Value.Should().Be(1);
        result[1].Status.Value.Should().Be("In Progress");
        result[1].Count.Value.Should().Be(1);
        capturedUrl.Should().Contain("status%20NOT%20IN");
        capturedUrl.Should().Contain("Done");
        capturedUrl.Should().Contain("Reject");
        capturedUrl.Should().Contain("Team");
        capturedUrl.Should().Contain("Import");
        capturedUrl.Should().Contain("fields=status,issuetype");
        capturedUrl.Should().NotContain("issuetype%20IN");
    }

    [Fact(DisplayName = "GetIssueCountsByStatusExcludingDoneAndRejectAsync uses status not-equals when reject is missing")]
    [Trait("Category", "Unit")]
    public async Task GetIssueCountsByStatusExcludingDoneAndRejectAsyncWhenRejectIsNullUsesStatusNotEqualsClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var client = CreateClient(transport.Object);

        // Act
        var result = await client.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            rejectStatusName: null,
            cts.Token);

        // Assert
        result.Should().BeEmpty();
        capturedUrl.Should().Contain("status%20%21%3D%20%22Done%22");
        capturedUrl.Should().NotContain("status%20NOT%20IN");
    }

    [Fact(DisplayName = "GetIssuesMovedToDoneThisMonthAsync includes issue links when requested")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesMovedToDoneThisMonthAsyncWhenIssueLinksRequestedMapsLinks()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "ADF-19423",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Covered supertask",
                        IssueLinks =
                        [
                            new JiraIssueLinkResponse
                            {
                                Type = new JiraIssueLinkTypeResponse { Inward = "is tested by" },
                                InwardIssue = new JiraIssueLinkIssueResponse { Key = "QA-7618" }
                            }
                        ]
                    }
                }
            ],
            IsLast = true,
            NextPageToken = null
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var client = CreateClient(transport.Object);

        // Act
        var issues = await client.GetIssuesMovedToDoneThisMonthAsync(
            new ProjectKey("ADF"),
            new StatusName("Done"),
            [new IssueTypeName("SuperTask")],
            cts.Token,
            includeIssueLinks: true);

        // Assert
        capturedUrl.Should().Contain("issuelinks");
        issues.Should().ContainSingle();
        issues[0].IssueLinks.Should().ContainSingle();
        issues[0].IssueLinks[0].Key.Value.Should().Be("QA-7618");
        issues[0].IssueLinks[0].RelationName.Should().Be("is tested by");
    }
    private static IOptions<AppSettings> CreateSettings(
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null,
        string? customFieldName = null,
        string? customFieldValue = null,
        string monthLabel = "2026-02",
        ReportPeriod? reportPeriod = null,
        string? pullRequestFieldName = null)
    {
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            reportPeriod ?? ReportPeriod.FromMonthLabel(new MonthLabel(monthLabel)),
            createdAfter: null,
            issueTypes: null,
            customFieldName: customFieldName,
            customFieldValue: customFieldValue,
            excludeWeekend: excludeWeekend,
            excludedDays: excludedDays,
            showGeneralStatistics: true,
            pullRequestFieldName: pullRequestFieldName);

        return Options.Create(settings);
    }
    private static JiraIssueSearchClient CreateClient(
        IJiraTransport transport,
        IOptions<AppSettings>? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        return new JiraIssueSearchClient(
            new JiraSearchExecutor(transport),
            new JiraJqlFacade(
                new TeamTasksJqlBuilder(resolvedSettings),
                new ReleaseIssuesJqlBuilder(resolvedSettings),
                new ArchTasksJqlBuilder(resolvedSettings),
                new GlobalIncidentsJqlBuilder(resolvedSettings)),
            new JiraFieldResolver(transport));
    }
}
