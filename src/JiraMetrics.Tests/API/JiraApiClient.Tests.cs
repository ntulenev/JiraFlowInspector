using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.API;
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
        Action act = () => _ = new JiraApiClient(transport, CreateSettings());

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

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

    [Fact(DisplayName = "GetIssueTimelineAsync returns mapped timeline")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenResponseIsValidReturnsMappedTimeline()
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
                    Created = "2026-02-01T10:00:00Z",
                    ResolutionDate = "2026-02-01T12:00:00Z"
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

        var client = new JiraApiClient(transport.Object, CreateSettings());

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.Key.Value.Should().Be("AAA-1");
        issue.IssueType.Value.Should().Be("Story");
        issue.Summary.Value.Should().Be("Fix bug");
        issue.PathKey.Value.Should().Be("OPEN->DONE");
        issue.PathLabel.Value.Should().Be("Open -> Done");
        issue.Transitions.Should().ContainSingle();
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

        var client = new JiraApiClient(transport.Object, CreateSettings(excludeWeekend: true));

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
        var client = new JiraApiClient(transport.Object, CreateSettings(excludedDays: excludedDays));

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

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

        var client = new JiraApiClient(transport.Object, CreateSettings());

        // Act
        Func<Task> act = () => client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }

    private static IOptions<AppSettings> CreateSettings(
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null)
    {
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StageName("Code Review"),
            new MonthLabel("2026-02"),
            createdAfter: null,
            issueTypes: null,
            excludeWeekend: excludeWeekend,
            excludedDays: excludedDays);

        return Options.Create(settings);
    }
}
