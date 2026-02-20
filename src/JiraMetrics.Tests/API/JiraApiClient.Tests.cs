using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.API;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraApiClientTests
{
    [Fact(DisplayName = "Constructor throws when transport is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransportIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraTransport transport = null!;

        // Act
        Action act = () => _ = CreateClient(transport, CreateSettings());

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

    [Fact(DisplayName = "GetIssueCountCreatedThisMonthAsync uses month range and bug issue type filter")]
    [Trait("Category", "Unit")]
    public async Task GetIssueCountCreatedThisMonthAsyncWhenCalledUsesCreatedAndIssueTypeClauses()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Total = 5,
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
        var count = await client.GetIssueCountCreatedThisMonthAsync(
            new ProjectKey("AAA"),
            [new IssueTypeName("Bug")],
            cts.Token);

        // Assert
        count.Value.Should().Be(5);
        capturedUrl.Should().Contain("created");
        capturedUrl.Should().Contain("2026-01-01");
        capturedUrl.Should().Contain("2026-02-01");
        capturedUrl.Should().Contain("issuetype");
        capturedUrl.Should().Contain("Bug");
        capturedUrl.Should().Contain("maxResults=1");
    }

    [Fact(DisplayName = "GetIssueCountMovedToDoneThisMonthAsync does not add created-after filter")]
    [Trait("Category", "Unit")]
    public async Task GetIssueCountMovedToDoneThisMonthAsyncWhenCalledDoesNotAddCreatedClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;

        var pageResponse = new JiraSearchResponse
        {
            Total = 3,
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
        var count = await client.GetIssueCountMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            [new IssueTypeName("Bug")],
            cts.Token);

        // Assert
        count.Value.Should().Be(3);
        capturedUrl.Should().Contain("status");
        capturedUrl.Should().Contain("CHANGED");
        capturedUrl.Should().Contain("status%20%3D%20%22Done%22");
        capturedUrl.Should().Contain("issuetype");
        capturedUrl.Should().Contain("Bug");
        capturedUrl.Should().NotContain("created%20%3E%3D");
        capturedUrl.Should().Contain("maxResults=1");
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
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
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
                                    Key = "ADF-100"
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
                                    Key = "ADF-101"
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
                                    Key = "ADF-999"
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
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-1");
        releases[0].Title.Value.Should().Be("Release item");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Tasks.Should().Be(2);
        capturedSearchUrl.Should().Contain("project");
        capturedSearchUrl.Should().Contain("RLS");
        capturedSearchUrl.Should().Contain("labels");
        capturedSearchUrl.Should().Contain("Processing");
        capturedSearchUrl.Should().Contain("Change%20completion%20date");
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("issuelinks");
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
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-2");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Tasks.Should().Be(0);
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
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var componentsJson = JsonDocument.Parse("[{\"value\":\"ADF PostgreSQL Database\"},{\"value\":\"Flux\"}]");
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
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_77777"] = componentsJson.RootElement.Clone()
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
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-3");
        releases[0].Components.Should().Be(2);
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("customfield_77777");
        capturedSearchUrl.Should().Contain("components");
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
        string monthLabel = "2026-02")
    {
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel(monthLabel),
            createdAfter: null,
            issueTypes: null,
            customFieldName: customFieldName,
            customFieldValue: customFieldValue,
            excludeWeekend: excludeWeekend,
            excludedDays: excludedDays);

        return Options.Create(settings);
    }

    private static JiraApiClient CreateClient(IJiraTransport transport, IOptions<AppSettings>? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        return new JiraApiClient(transport, resolvedSettings, CreateTransitionBuilder(resolvedSettings));
    }

    private static TransitionBuilder CreateTransitionBuilder(IOptions<AppSettings> settings) => new(settings);
}
