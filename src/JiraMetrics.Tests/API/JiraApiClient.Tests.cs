using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.API;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

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
        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new JiraApiClient(transport, analytics);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when analytics service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAnalyticsServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict).Object;
        IJiraAnalyticsService analytics = null!;

        // Act
        Action act = () => _ = new JiraApiClient(transport, analytics);

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

        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict).Object;
        var client = new JiraApiClient(transport.Object, analytics);

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

        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict).Object;
        var client = new JiraApiClient(transport.Object, analytics);

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

        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict).Object;
        var client = new JiraApiClient(transport.Object, analytics);

        // Act
        var keys = await client.GetIssueKeysMovedToDoneThisMonthAsync(
            new ProjectKey("AAA"),
            new StatusName("Done"),
            cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        keys.Select(key => key.Value).Should().ContainInOrder("aaa-1", "AAA-2", "AAA-3");
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

        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict);
        analytics.Setup(x => x.BuildPathKey(It.IsAny<IReadOnlyList<JiraMetrics.Models.TransitionEvent>>()))
            .Returns(new PathKey("OPEN->DONE"));
        analytics.Setup(x => x.BuildPathLabel(It.IsAny<IReadOnlyList<JiraMetrics.Models.TransitionEvent>>()))
            .Returns(new PathLabel("Open -> Done"));

        var client = new JiraApiClient(transport.Object, analytics.Object);

        // Act
        var issue = await client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        issue.Key.Value.Should().Be("AAA-1");
        issue.Summary.Value.Should().Be("Fix bug");
        issue.PathKey.Value.Should().Be("OPEN->DONE");
        issue.PathLabel.Value.Should().Be("Open -> Done");
        issue.Transitions.Should().ContainSingle();
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

        var analytics = new Mock<IJiraAnalyticsService>(MockBehavior.Strict).Object;
        var client = new JiraApiClient(transport.Object, analytics);

        // Act
        Func<Task> act = () => client.GetIssueTimelineAsync(new IssueKey("AAA-1"), cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }
}
