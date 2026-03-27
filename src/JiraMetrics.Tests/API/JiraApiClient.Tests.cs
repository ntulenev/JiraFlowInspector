using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraApiClientTests
{
    [Fact(DisplayName = "Constructor throws when search executor is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSearchExecutorIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraSearchExecutor searchExecutor = null!;

        // Act
        Action act = () => _ = new JiraApiClient(
            searchExecutor,
            Mock.Of<IJiraJqlFacade>(),
            CreateSettings(),
            Mock.Of<IJiraFieldResolver>(),
            Mock.Of<IJiraMapperFacade>());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetCurrentUserAsync returns mapped user when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetCurrentUserAsyncWhenResponseIsValidReturnsMappedUser()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var requestUrl = new Uri("rest/api/3/myself", UriKind.Relative);

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport.Setup(t => t.GetAsync<JiraCurrentUserResponse>(
                It.Is<Uri>(u => u == requestUrl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraCurrentUserResponse
            {
                DisplayName = "Jane Doe",
                EmailAddress = "user@example.com",
                AccountId = "123"
            });

        var client = CreateClient(transport.Object);

        // Act
        var user = await client.GetCurrentUserAsync(cts.Token);

        // Assert
        user.DisplayName.Value.Should().Be("Jane Doe");
        user.EmailAddress.Should().Be("user@example.com");
        user.AccountId.Should().Be("123");
    }

    [Fact(DisplayName = "GetCurrentUserAsync throws when response body is null")]
    [Trait("Category", "Unit")]
    public async Task GetCurrentUserAsyncWhenResponseBodyIsNullThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var requestUrl = new Uri("rest/api/3/myself", UriKind.Relative);

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport.Setup(t => t.GetAsync<JiraCurrentUserResponse>(
                It.Is<Uri>(u => u == requestUrl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((JiraCurrentUserResponse?)null);

        var client = CreateClient(transport.Object);

        // Act
        Func<Task> act = () => client.GetCurrentUserAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

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
                It.IsAny<CancellationToken>()))
            .Callback(() => sendCalls++)
            .ReturnsAsync(firstResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.Is<Uri>(u => u.ToString().Contains("nextPageToken=next-1", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
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
                It.IsAny<CancellationToken>()))
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
                It.IsAny<CancellationToken>()))
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
                It.IsAny<CancellationToken>()))
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
                It.IsAny<CancellationToken>()))
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
                        Created = "2026-01-10T09:00:00Z"
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
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
        capturedUrl.Should().Contain("created");
        capturedUrl.Should().Contain("fields=key,summary,created");
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
                        Created = "2026-01-11T10:00:00Z"
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
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
        capturedUrl.Should().Contain("status");
        capturedUrl.Should().Contain("status%20%3D%20%22Done%22");
        capturedUrl.Should().Contain("fields=key,summary,created");
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
                It.IsAny<CancellationToken>()))
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
                It.IsAny<CancellationToken>()))
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

    [Fact(DisplayName = "GetArchTasksAsync returns created and resolved timestamps")]
    [Trait("Category", "Unit")]
    public async Task GetArchTasksAsyncWhenCalledReturnsArchitectureTasks()
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
                    Key = "AAA-7",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Architecture review",
                        Created = "2026-03-02T09:30:00Z",
                        ResolutionDate = "2026-03-05T12:00:00Z"
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-03");
        var client = CreateClient(transport.Object, settings);

        // Act
        var tasks = await client.GetArchTasksAsync(
            new ArchTasksReportSettings(
                "project = AAA AND type = \"Arch Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC"),
            cts.Token);

        // Assert
        tasks.Should().ContainSingle();
        tasks[0].Key.Value.Should().Be("AAA-7");
        tasks[0].Title.Value.Should().Be("Architecture review");
        tasks[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 3, 2, 9, 30, 0, TimeSpan.Zero));
        tasks[0].ResolvedAt.Should().Be(new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero));
        capturedUrl.Should().Contain("resolved");
        capturedUrl.Should().Contain("2026-03-01");
        capturedUrl.Should().Contain("2026-04-01");
        capturedUrl.Should().Contain("fields=key,summary,created,resolutiondate");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync uses release project, label and release date field")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenCalledReturnsReleaseIssues()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var environmentJson = JsonDocument.Parse("[{\"value\":\"P005\"}]");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-1",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Release item",
                        Status = new JiraIssueStatusResponse { Name = "Ready for Prod" },
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
                                    Key = "NOVA-100"
                                }
                            },
                            new JiraIssueLinkResponse
                            {
                                Type = new JiraIssueLinkTypeResponse
                                {
                                    Inward = "is caused by"
                                },
                                InwardIssue = new JiraIssueLinkIssueResponse
                                {
                                    Key = "NOVA-101"
                                }
                            },
                            new JiraIssueLinkResponse
                            {
                                Type = new JiraIssueLinkTypeResponse
                                {
                                    Inward = "relates to"
                                },
                                InwardIssue = new JiraIssueLinkIssueResponse
                                {
                                    Key = "NOVA-999"
                                }
                            }
                        ],
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            null,
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            "Rollback type",
            environmentFieldName: null,
            environmentFieldValue: null,
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-1");
        releases[0].Title.Value.Should().Be("Release item");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Status.Value.Should().Be("Ready for Prod");
        releases[0].Tasks.Should().Be(3);
        releases[0].ComponentNames.Should().BeEmpty();
        releases[0].EnvironmentNames.Should().BeEmpty();
        releases[0].IsHotFix.Should().BeFalse();
        capturedSearchUrl.Should().Contain("project");
        capturedSearchUrl.Should().Contain("RLS");
        capturedSearchUrl.Should().Contain("labels");
        capturedSearchUrl.Should().Contain("Processing");
        capturedSearchUrl.Should().Contain("Change%20completion%20date");
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("status");
        capturedSearchUrl.Should().Contain("issuelinks");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync adds environment filter when configured")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenEnvironmentFilterIsConfiguredAddsClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var environmentJson = JsonDocument.Parse("[{\"value\":\"P005\"}]");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-7",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "P005 release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_10865"] = environmentJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            componentsFieldName: null,
            hotFixRules: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            rollbackFieldName: "Rollback type",
            environmentFieldName: "customfield_10865",
            environmentFieldValue: "P005",
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-7");
        releases[0].EnvironmentNames.Should().ContainSingle().Which.Should().Be("P005");
        capturedSearchUrl.Should().Contain("customfield_10865");
        capturedSearchUrl.Should().Contain("P005");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync parses datetime release field values")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenReleaseDateIsDateTimeParsesDatePart()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14T23:10:00+02:00\"");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-2",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Release with timestamp",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            null,
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            "Rollback type",
            environmentFieldName: null,
            environmentFieldValue: null,
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-2");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Status.Should().Be(StatusName.Unknown);
        releases[0].Tasks.Should().Be(0);
        releases[0].ComponentNames.Should().BeEmpty();
        releases[0].EnvironmentNames.Should().BeEmpty();
        releases[0].IsHotFix.Should().BeFalse();
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync counts components when components field is configured")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenComponentsFieldIsConfiguredCountsComponents()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_77777",
                Name = "Component/s"
            },
            new()
            {
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var componentsJson = JsonDocument.Parse("[{\"value\":\"Nebula PostgreSQL Database\"},{\"value\":\"Flux\"}]");
        using var environmentsJson = JsonDocument.Parse("[{\"value\":\"P005\"},{\"value\":\"S005\"}]");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-3",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Release with components",
                        Status = new JiraIssueStatusResponse { Name = "Released" },
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_77777"] = componentsJson.RootElement.Clone(),
                            ["customfield_10865"] = environmentsJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            "Components",
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            "Rollback type",
            environmentFieldName: "customfield_10865",
            environmentFieldValue: "P005",
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-3");
        releases[0].Status.Value.Should().Be("Released");
        releases[0].Components.Should().Be(2);
        releases[0].ComponentNames.Should().ContainInOrder("Flux", "Nebula PostgreSQL Database");
        releases[0].EnvironmentNames.Should().ContainInOrder("P005", "S005");
        releases[0].IsHotFix.Should().BeFalse();
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("customfield_77777");
        capturedSearchUrl.Should().Contain("customfield_10865");
        capturedSearchUrl.Should().Contain("status");
        capturedSearchUrl.Should().Contain("components");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync maps rollback payload when rollback field contains value")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenRollbackFieldContainsValueMapsRollbackPayload()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_66666",
                Name = "Rollback type"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-17\"");
        using var rollbackJson = JsonDocument.Parse("{\"value\":\"Full rollback\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-6",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Rollback release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_66666"] = rollbackJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            componentsFieldName: null,
            hotFixRules: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            rollbackFieldName: "Rollback type",
            environmentFieldName: null,
            environmentFieldValue: null,
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-6");
        releases[0].RollbackType.Should().Be("Full rollback");
        capturedSearchUrl.Should().Contain("customfield_66666");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync marks release as hot-fix when configured field matches value")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenHotFixFieldMatchesMarksReleaseAsHotFix()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_88888",
                Name = "Change type"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-16\"");
        using var hotFixJson = JsonDocument.Parse("{\"value\":\"Emergency\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-4",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Emergency release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_88888"] = hotFixJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            componentsFieldName: null,
            hotFixRules: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            },
            rollbackFieldName: "Rollback type",
            environmentFieldName: null,
            environmentFieldValue: null,
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-4");
        releases[0].IsHotFix.Should().BeTrue();
        capturedSearchUrl.Should().Contain("customfield_88888");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync marks release as hot-fix when any configured rule matches")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenAnyHotFixRuleMatchesMarksReleaseAsHotFix()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_12345",
                Name = "Change completion date"
            },
            new()
            {
                Id = "customfield_88888",
                Name = "Change type"
            },
            new()
            {
                Id = "customfield_99999",
                Name = "Change reason"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-16\"");
        using var changeReasonJson = JsonDocument.Parse("{\"value\":\"Mitigation\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-5",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Mitigation release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_99999"] = changeReasonJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            componentsFieldName: null,
            hotFixRules: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"],
                ["Change reason"] = ["Repair", "Mitigation"]
            },
            rollbackFieldName: "Rollback type",
            environmentFieldName: null,
            environmentFieldValue: null,
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-5");
        releases[0].IsHotFix.Should().BeTrue();
        capturedSearchUrl.Should().Contain("customfield_88888");
        capturedSearchUrl.Should().Contain("customfield_99999");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync uses namespace and configured JQL filter")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenJqlFilterIsConfiguredAddsRawClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_10802",
                Name = "Impact"
            },
            new()
            {
                Id = "customfield_10847",
                Name = "Urgency"
            },
            new()
            {
                Id = "customfield_11348",
                Name = "Business Impact"
            }
        };

        using var incidentStartJson = JsonDocument.Parse("\"2026-02-12 10:00\"");
        using var incidentRecoveryJson = JsonDocument.Parse("\"2026-02-12 10:49\"");
        using var impactJson = JsonDocument.Parse("{\"value\":\"Significant / Large\"}");
        using var urgencyJson = JsonDocument.Parse("{\"value\":\"High\"}");
        using var businessImpactJson = JsonDocument.Parse("{\"type\":\"doc\",\"version\":1,\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"ORX live feed unavailable\"}]}]}");

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "INC-11861",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "NOVA - ORX disabled 10/03/2026",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_14851"] = incidentStartJson.RootElement.Clone(),
                            ["customfield_14852"] = incidentRecoveryJson.RootElement.Clone(),
                            ["customfield_10802"] = impactJson.RootElement.Clone(),
                            ["customfield_10847"] = urgencyJson.RootElement.Clone(),
                            ["customfield_11348"] = businessImpactJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var incidents = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(
                namespaceName: "Incidents",
                jqlFilter: "(\"Incident categorization\" = ORX OR labels = ORX OR summary ~ \"ORX\") AND (summary ~ \"disab*\" OR summary ~ \"unavail*\" OR summary ~ \"downtime\")",
                additionalFieldNames: ["Business Impact"]),
            cts.Token);

        // Assert
        incidents.Should().ContainSingle();
        incidents[0].Key.Value.Should().Be("INC-11861");
        incidents[0].Impact.Should().Be("Significant / Large");
        incidents[0].Urgency.Should().Be("High");
        incidents[0].Duration.Should().Be(TimeSpan.FromMinutes(49));
        incidents[0].AdditionalFields.Should().ContainKey("Business Impact");
        incidents[0].AdditionalFields["Business Impact"].Should().Be("ORX live feed unavailable");
        capturedSearchUrl.Should().Contain("project%20%3D%20%22Incidents%22");
        capturedSearchUrl.Should().Contain("Incident%20categorization");
        capturedSearchUrl.Should().Contain("labels%20%3D%20ORX");
        capturedSearchUrl.Should().Contain("summary%20~%20%22ORX%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22disab%2A%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22unavail%2A%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22downtime%22");
        capturedSearchUrl.Should().Contain("customfield_14851");
        capturedSearchUrl.Should().Contain("customfield_11348");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync uses fallback start and recovery fields when primary fields are empty")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenPrimaryDateFieldsAreEmptyUsesFallbackFields()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_11416",
                Name = "Incident Start date/time user timezone"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_11417",
                Name = "Incident Recovery date/time user timezone"
            },
            new()
            {
                Id = "customfield_11348",
                Name = "Business Impact"
            }
        };

        using var fallbackStartJson = JsonDocument.Parse("\"2026-01-12T03:52:00.000+0100\"");
        using var fallbackRecoveryJson = JsonDocument.Parse("\"2026-01-12T04:49:00.000+0100\"");
        using var businessImpactJson = JsonDocument.Parse("{\"type\":\"doc\",\"version\":1,\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"ADF services were unavailable due to Google maintenance\"}]}]}");

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "INC-11586",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "SB2 - ADF disablement since services was down 12/01/2025",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_11416"] = fallbackStartJson.RootElement.Clone(),
                            ["customfield_11417"] = fallbackRecoveryJson.RootElement.Clone(),
                            ["customfield_11348"] = businessImpactJson.RootElement.Clone()
                        }
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-01");
        var client = CreateClient(transport.Object, settings);

        // Act
        var incidents = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(
                namespaceName: "Incidents",
                jqlFilter: "labels = ADF AND summary ~ \"disab*\"",
                incidentStartFallbackFieldName: "Incident Start date/time user timezone",
                incidentRecoveryFallbackFieldName: "Incident Recovery date/time user timezone",
                additionalFieldNames: ["Business Impact"]),
            cts.Token);

        // Assert
        incidents.Should().ContainSingle();
        incidents[0].Key.Value.Should().Be("INC-11586");
        incidents[0].IncidentStartUtc.Should().Be(new DateTimeOffset(2026, 1, 12, 2, 52, 0, TimeSpan.Zero));
        incidents[0].IncidentRecoveryUtc.Should().Be(new DateTimeOffset(2026, 1, 12, 3, 49, 0, TimeSpan.Zero));
        incidents[0].Duration.Should().Be(TimeSpan.FromMinutes(57));
        capturedSearchUrl.Should().Contain("customfield_14851");
        capturedSearchUrl.Should().Contain("customfield_11416");
        capturedSearchUrl.Should().Contain("customfield_14852");
        capturedSearchUrl.Should().Contain("customfield_11417");
        capturedSearchUrl.Should().Contain("Incident%20Start%20date%2Ftime%20user%20timezone%22%20%3E%3D%20%222026-01-01");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync falls back to search phrase when JQL filter is missing")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenJqlFilterIsMissingUsesSearchPhraseTerms()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_10802",
                Name = "Impact"
            },
            new()
            {
                Id = "customfield_10847",
                Name = "Urgency"
            }
        };

        var pageResponse = new JiraSearchResponse
        {
            Issues = [],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        _ = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(namespaceName: "Incidents", searchPhrase: "ORX disab"),
            cts.Token);

        // Assert
        capturedSearchUrl.Should().Contain("text%20~%20%22ORX%2A%22");
        capturedSearchUrl.Should().Contain("text%20~%20%22disab%2A%22");
    }

    [Fact(DisplayName = "GetIssueTimelineAsync returns mapped timeline")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenResponseIsValidReturnsMappedTimeline()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var developmentField = JsonDocument.Parse("{\"pullrequest\":{\"overall\":{\"count\":1}}}").RootElement.Clone();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z",
                    AdditionalFields = new Dictionary<string, JsonElement>
                    {
                        ["customfield_10800"] = developmentField
                    },
                    Subtasks =
                    [
                        new JiraSubtaskResponse { Key = "AAA-2" },
                        new JiraSubtaskResponse { Key = "AAA-3" }
                    ]
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories =
                    [
                        new JiraHistoryResponse
                        {
                            Created = "2026-02-01T11:00:00Z",
                            Items = [new JiraHistoryItemResponse { Field = "status", FromStatus = "Open", ToStatus = "Done" }]
                        }
                    ]
                }
            });

        var client = CreateClient(transport.Object);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.Key.Value.Should().Be("AAA-1");
        issue.IssueType.Value.Should().Be("Story");
        issue.Summary.Value.Should().Be("Fix bug");
        issue.PathKey.Value.Should().Be("OPEN->DONE");
        issue.PathLabel.Value.Should().Be("Open -> Done");
        issue.SubItemsCount.Should().Be(2);
        issue.HasPullRequest.Should().BeTrue();
        issue.Transitions.Should().ContainSingle();
    }

    [Fact(DisplayName = "GetIssueTimelinesAsync uses bulk endpoints and maps per-key failures")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelinesAsyncWhenBulkResponsesAreValidReturnsMappedIssuesAndMissingFailures()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var developmentField = JsonDocument.Parse("{\"pullrequest\":{\"overall\":{\"count\":1}}}").RootElement.Clone();
        var changelogCreatedAt = new DateTimeOffset(2026, 2, 1, 11, 0, 0, TimeSpan.Zero);
        var changelogCreatedAtUnixMilliseconds = changelogCreatedAt.ToUnixTimeMilliseconds();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.PostAsync<JiraBulkIssueFetchRequest, JiraBulkIssueFetchResponse>(
                It.Is<Uri>(u => u.ToString() == "rest/api/3/issue/bulkfetch"),
                It.Is<JiraBulkIssueFetchRequest>(request =>
                    request.IssueIdsOrKeys.SequenceEqual(_bulkTimelineIssueKeys)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraBulkIssueFetchResponse
            {
                Issues =
                [
                    new JiraIssueResponse
                    {
                        Id = "10001",
                        Key = "AAA-1",
                        Fields = new JiraIssueFieldsResponse
                        {
                            Summary = "Fix bug",
                            IssueType = new JiraIssueTypeResponse { Name = "Story" },
                            Created = "2026-02-01T10:00:00Z",
                            ResolutionDate = "2026-02-01T12:00:00Z",
                            AdditionalFields = new Dictionary<string, JsonElement>
                            {
                                ["customfield_10800"] = developmentField
                            }
                        }
                    }
                ]
            });
        transport
            .Setup(t => t.PostAsync<JiraBulkChangelogFetchRequest, JiraBulkChangelogFetchResponse>(
                It.Is<Uri>(u => u.ToString() == "rest/api/3/changelog/bulkfetch"),
                It.Is<JiraBulkChangelogFetchRequest>(request =>
                    request.IssueIdsOrKeys.SequenceEqual(_bulkTimelineIssueKeys)
                    && request.MaxResults == 1000
                    && request.NextPageToken == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraBulkChangelogFetchResponse
            {
                IssueChangeLogs =
                [
                    new JiraBulkIssueChangelogResponse
                    {
                        IssueId = "10001",
                        ChangeHistories =
                        [
                            new JiraBulkHistoryResponse
                            {
                                Created = JsonDocument.Parse($"{changelogCreatedAtUnixMilliseconds}").RootElement.Clone(),
                                Items =
                                [
                                    new JiraHistoryItemResponse
                                    {
                                        Field = "status",
                                        FromStatus = "Open",
                                        ToStatus = "Done"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            });

        var client = CreateClient(transport.Object);

        // Act
        var result = await client.GetIssueTimelinesAsync(
            [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            cts.Token);

        // Assert
        result.Issues.Should().ContainSingle();
        result.Issues[0].Key.Value.Should().Be("AAA-1");
        result.Issues[0].HasPullRequest.Should().BeTrue();
        result.Issues[0].Transitions.Should().ContainSingle();
        result.Issues[0].Transitions[0].At.Should().Be(changelogCreatedAt);
        result.Failures.Should().ContainSingle();
        result.Failures[0].IssueKey.Value.Should().Be("AAA-2");
        result.Failures[0].Reason.Value.Should().Contain("not returned by Jira bulk fetch");
    }

    [Fact(DisplayName = "GetIssueTimelineAsync sets no pull request flag when development pullrequest count is zero")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenDevelopmentPullRequestCountIsZeroSetsNoPullRequestFlag()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var developmentField = JsonDocument.Parse("{\"pullrequest\":{\"overall\":{\"count\":0}}}").RootElement.Clone();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z",
                    AdditionalFields = new Dictionary<string, JsonElement>
                    {
                        ["customfield_10800"] = developmentField
                    }
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories = []
                }
            });

        var client = CreateClient(transport.Object);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.HasPullRequest.Should().BeFalse();
    }

    [Fact(DisplayName = "GetIssueTimelineAsync uses configured pull request field name")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenPullRequestFieldNameIsConfiguredUsesConfiguredField()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;
        var developmentField = JsonDocument.Parse("{\"pullrequest\":{\"overall\":{\"count\":1}}}").RootElement.Clone();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z",
                    AdditionalFields = new Dictionary<string, JsonElement>
                    {
                        ["customfield_99999"] = developmentField
                    }
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories = []
                }
            });

        var settings = CreateSettings(pullRequestFieldName: "customfield_99999");
        var client = CreateClient(transport.Object, settings);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.HasPullRequest.Should().BeTrue();
        capturedUrl.Should().Contain("expand=changelog");
        capturedUrl.Should().Contain("fields=summary,created,resolutiondate,issuetype,status,issuelinks,subtasks,customfield_99999");
    }

    [Fact(DisplayName = "GetIssueTimelineAsync detects pull request when pull request field name is not configured")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenPullRequestFieldNameIsMissingScansAllAdditionalFields()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;
        var developmentField = JsonDocument.Parse("{\"pullrequest\":{\"overall\":{\"count\":1}}}").RootElement.Clone();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z",
                    AdditionalFields = new Dictionary<string, JsonElement>
                    {
                        ["customfield_55555"] = developmentField
                    }
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories = []
                }
            });

        var settings = CreateSettings(pullRequestFieldName: null);
        var client = CreateClient(transport.Object, settings);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.HasPullRequest.Should().BeTrue();
        capturedUrl.Should().Contain("expand=changelog");
        capturedUrl.Should().NotContain("&fields=");
    }

    [Fact(DisplayName = "GetIssueTimelineAsync excludes weekends when configured")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenExcludeWeekendIsTrueSkipsWeekendHours()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-06T10:00:00Z",
                    ResolutionDate = "2026-02-09T10:00:00Z"
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories =
                    [
                        new JiraHistoryResponse
                        {
                            Created = "2026-02-09T10:00:00Z",
                            Items = [new JiraHistoryItemResponse { Field = "status", FromStatus = "Open", ToStatus = "Done" }]
                        }
                    ]
                }
            });

        var settings = CreateSettings(excludeWeekend: true);
        var client = CreateClient(transport.Object, settings);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.Transitions.Should()
            .ContainSingle()
            .Which.SincePrevious.Should()
            .Be(TimeSpan.FromHours(24));
    }

    [Fact(DisplayName = "GetIssueTimelineAsync excludes configured holidays")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenExcludedDaysAreConfiguredSkipsThoseHours()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = new JiraIssueTypeResponse { Name = "Story" },
                    Created = "2026-02-02T10:00:00Z",
                    ResolutionDate = "2026-02-04T10:00:00Z"
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories =
                    [
                        new JiraHistoryResponse
                        {
                            Created = "2026-02-04T10:00:00Z",
                            Items = [new JiraHistoryItemResponse { Field = "status", FromStatus = "Open", ToStatus = "Done" }]
                        }
                    ]
                }
            });

        var excludedDays = new List<DateOnly> { new(2026, 2, 3) };
        var settings = CreateSettings(excludedDays: excludedDays);
        var client = CreateClient(transport.Object, settings);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.Transitions.Should()
            .ContainSingle()
            .Which.SincePrevious.Should()
            .Be(TimeSpan.FromHours(24));
    }

    [Fact(DisplayName = "GetIssueTimelineAsync uses unknown issue type when response issue type is missing")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenIssueTypeIsMissingUsesUnknownIssueType()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = new JiraIssueFieldsResponse
                {
                    Summary = "Fix bug",
                    IssueType = null,
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z"
                },
                Changelog = new JiraChangelogResponse
                {
                    Histories = []
                }
            });

        var client = CreateClient(transport.Object);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.IssueType.Should().Be(IssueTypeName.Unknown);
    }

    [Fact(DisplayName = "GetIssueTimelineAsync throws when fields are missing")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenFieldsAreMissingThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraIssueResponse>(
                It.IsAny<Uri>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JiraIssueResponse
            {
                Key = "AAA-1",
                Fields = null
            });

        var client = CreateClient(transport.Object);

        // Act
        Func<Task> act = () => client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();
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

    private static JiraApiClient CreateClient(IJiraTransport transport, IOptions<AppSettings>? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        var fieldValueReader = new JiraFieldValueReader();

        return new JiraApiClient(
            new JiraSearchExecutor(transport),
            new JiraJqlFacade(
                new TeamTasksJqlBuilder(resolvedSettings),
                new ReleaseIssuesJqlBuilder(resolvedSettings),
                new ArchTasksJqlBuilder(resolvedSettings),
                new GlobalIncidentsJqlBuilder(resolvedSettings)),
            resolvedSettings,
            new JiraFieldResolver(transport),
            new JiraMapperFacade(
                new IssueTimelineMapper(CreateTransitionBuilder(resolvedSettings), resolvedSettings, fieldValueReader),
                new ReleaseIssueMapper(fieldValueReader),
                new GlobalIncidentMapper(fieldValueReader)));
    }

    private static TransitionBuilder CreateTransitionBuilder(IOptions<AppSettings> settings) => new(settings);

    private static readonly string[] _bulkTimelineIssueKeys = ["AAA-1", "AAA-2"];
}

