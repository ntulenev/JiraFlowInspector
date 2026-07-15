using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraIssueTimelineClientTests
{
    [Fact(DisplayName = "Batch timeline load returns empty result without dependencies")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelinesAsyncWhenKeysAreEmptyReturnsEmptyResult()
    {
        // Arrange
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        var fieldResolver = new Mock<IJiraFieldResolver>(MockBehavior.Strict);
        var mapper = new Mock<IJiraMapperFacade>(MockBehavior.Strict);
        var client = CreateClient(searchExecutor.Object, fieldResolver.Object, mapper.Object);

        // Act
        var result = await client.GetIssueTimelinesAsync([], CancellationToken.None);

        // Assert
        result.Issues.Should().BeEmpty();
        result.Failures.Should().BeEmpty();
        searchExecutor.VerifyNoOtherCalls();
        fieldResolver.VerifyNoOtherCalls();
        mapper.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Single timeline load rejects empty Jira response")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenResponseIsNullThrowsInvalidOperationException()
    {
        // Arrange
        var issueKey = new IssueKey("FLOW-1");
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        searchExecutor.Setup(e => e.GetIssueWithChangelogAsync(issueKey, null, CancellationToken.None))
            .ReturnsAsync((JiraIssueResponse?)null);
        var client = CreateClient(
            searchExecutor.Object,
            new Mock<IJiraFieldResolver>(MockBehavior.Strict).Object,
            new Mock<IJiraMapperFacade>(MockBehavior.Strict).Object);

        // Act
        Task<IssueTimeline> Act()
        {
            return client.GetIssueTimelineAsync(issueKey, CancellationToken.None);
        }

        // Assert
        await FluentActions.Awaiting(Act).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Jira issue response is empty.");
    }

    [Fact(DisplayName = "Timeline load requests configured custom pull request field directly")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenPullRequestFieldIsIdBypassesResolver()
    {
        // Arrange
        var issueKey = new IssueKey("FLOW-2");
        var response = new JiraIssueResponse { Key = issueKey.Value };
        var expected = CreateTimeline(issueKey);
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        searchExecutor.Setup(e => e.GetIssueWithChangelogAsync(
                issueKey,
                It.Is<JiraSearchFields>(fields =>
                    fields.Count == 8
                    && fields.Contains("customfield_10800")),
                CancellationToken.None))
            .ReturnsAsync(response);
        var fieldResolver = new Mock<IJiraFieldResolver>(MockBehavior.Strict);
        var mapper = new Mock<IJiraMapperFacade>(MockBehavior.Strict);
        mapper.Setup(m => m.MapIssueTimeline(response, issueKey)).Returns(expected);
        var client = CreateClient(
            searchExecutor.Object,
            fieldResolver.Object,
            mapper.Object,
            "customfield_10800");

        // Act
        var result = await client.GetIssueTimelineAsync(issueKey, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        fieldResolver.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Timeline client caches pull request field name resolution")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelineAsyncWhenPullRequestFieldIsNameResolvesItOnce()
    {
        // Arrange
        var issueKey = new IssueKey("FLOW-3");
        var response = new JiraIssueResponse { Key = issueKey.Value };
        var expected = CreateTimeline(issueKey);
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        searchExecutor.Setup(e => e.GetIssueWithChangelogAsync(
                issueKey,
                It.Is<JiraSearchFields>(fields => fields.Contains("customfield_10800")),
                CancellationToken.None))
            .ReturnsAsync(response);
        var fieldResolver = new Mock<IJiraFieldResolver>(MockBehavior.Strict);
        fieldResolver.Setup(r => r.TryResolveFieldIdAsync(
                new JiraFieldName("Development"),
                CancellationToken.None))
            .ReturnsAsync(new JiraFieldId("customfield_10800"));
        var mapper = new Mock<IJiraMapperFacade>(MockBehavior.Strict);
        mapper.Setup(m => m.MapIssueTimeline(response, issueKey)).Returns(expected);
        var client = CreateClient(
            searchExecutor.Object,
            fieldResolver.Object,
            mapper.Object,
            "Development");

        // Act
        _ = await client.GetIssueTimelineAsync(issueKey, CancellationToken.None);
        _ = await client.GetIssueTimelineAsync(issueKey, CancellationToken.None);

        // Assert
        fieldResolver.Verify(
            r => r.TryResolveFieldIdAsync(new JiraFieldName("Development"), CancellationToken.None),
            Times.Once);
    }

    [Theory(DisplayName = "Batch timeline load converts supported batch exceptions to per-key failures")]
    [Trait("Category", "Unit")]
    [InlineData("http")]
    [InlineData("invalid-operation")]
    [InlineData("json")]
    public async Task GetIssueTimelinesAsyncWhenBatchFailsReturnsFailureForEveryKey(string failureKind)
    {
        // Arrange
        var issueKeys = new[] { new IssueKey("FLOW-4"), new IssueKey("FLOW-5") };
        Exception exception = failureKind switch
        {
            "http" => new HttpRequestException("transport failed"),
            "invalid-operation" => new InvalidOperationException("payload failed"),
            "json" => new JsonException("json failed"),
            _ => throw new InvalidOperationException("Unsupported test failure kind.")
        };
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        searchExecutor.Setup(e => e.GetIssuesAsync(
                It.Is<IReadOnlyList<IssueKey>>(keys => keys.SequenceEqual(issueKeys)),
                null,
                CancellationToken.None))
            .ThrowsAsync(exception);
        var client = CreateClient(
            searchExecutor.Object,
            new Mock<IJiraFieldResolver>(MockBehavior.Strict).Object,
            new Mock<IJiraMapperFacade>(MockBehavior.Strict).Object);

        // Act
        var result = await client.GetIssueTimelinesAsync(issueKeys, CancellationToken.None);

        // Assert
        result.Issues.Should().BeEmpty();
        result.Failures.Select(static failure => failure.IssueKey).Should().Equal(issueKeys);
        result.Failures.Should().OnlyContain(failure => failure.Reason.Value.Contains("failed", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Batch timeline load isolates mapping and missing issue failures")]
    [Trait("Category", "Unit")]
    public async Task GetIssueTimelinesAsyncWhenResultsArePartialReturnsIssuesAndPerKeyFailures()
    {
        // Arrange
        var firstKey = new IssueKey("FLOW-6");
        var invalidKey = new IssueKey("FLOW-7");
        var missingKey = new IssueKey("FLOW-8");
        var issueKeys = new[] { firstKey, invalidKey, missingKey };
        var firstResponse = new JiraIssueResponse { Id = "10006", Key = firstKey.Value };
        var invalidResponse = new JiraIssueResponse { Id = "10007", Key = invalidKey.Value };
        var blankResponse = new JiraIssueResponse { Id = "10008", Key = " " };
        var history = new JiraHistoryResponse { Created = "2026-07-01T10:00:00Z" };
        var expected = CreateTimeline(firstKey);

        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict);
        searchExecutor.Setup(e => e.GetIssuesAsync(
                It.Is<IReadOnlyList<IssueKey>>(keys => keys.SequenceEqual(issueKeys)),
                null,
                CancellationToken.None))
            .ReturnsAsync([firstResponse, invalidResponse, blankResponse]);
        searchExecutor.Setup(e => e.GetIssueChangelogsAsync(
                It.Is<IReadOnlyList<IssueKey>>(keys => keys.SequenceEqual(issueKeys)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<JiraHistoryResponse>>
            {
                ["10006"] = [history]
            });

        var mapper = new Mock<IJiraMapperFacade>(MockBehavior.Strict);
        mapper.Setup(m => m.MapIssueTimeline(
                It.Is<JiraIssueResponse>(response =>
                    response.Key == firstKey.Value
                    && response.Changelog != null
                    && response.Changelog.Histories.Count == 1
                    && ReferenceEquals(response.Changelog.Histories[0], history)),
                firstKey))
            .Returns(expected);
        mapper.Setup(m => m.MapIssueTimeline(
                It.Is<JiraIssueResponse>(response => response.Key == invalidKey.Value),
                invalidKey))
            .Throws(new InvalidOperationException("invalid issue payload"));
        var client = CreateClient(
            searchExecutor.Object,
            new Mock<IJiraFieldResolver>(MockBehavior.Strict).Object,
            mapper.Object);

        // Act
        var result = await client.GetIssueTimelinesAsync(issueKeys, CancellationToken.None);

        // Assert
        result.Issues.Should().ContainSingle().Which.Should().BeSameAs(expected);
        result.Failures.Should().HaveCount(2);
        result.Failures.Should().Contain(failure =>
            failure.IssueKey == invalidKey
            && failure.Reason.Value.Contains("invalid issue payload", StringComparison.Ordinal));
        result.Failures.Should().Contain(failure =>
            failure.IssueKey == missingKey
            && failure.Reason.Value.Contains("not returned", StringComparison.Ordinal));
    }

    private static JiraIssueTimelineClient CreateClient(
        IJiraSearchExecutor searchExecutor,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapper,
        string? pullRequestFieldName = null) =>
        new(
            searchExecutor,
            Options.Create(CreateSettings(pullRequestFieldName)),
            fieldResolver,
            mapper);

    private static AppSettings CreateSettings(string? pullRequestFieldName) =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.test"),
            new JiraApiToken("token"),
            new ProjectKey("FLOW"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-07"),
            pullRequestFieldName: pullRequestFieldName);

    private static IssueTimeline CreateTimeline(IssueKey issueKey) =>
        IssueTimeline.Create(
            issueKey,
            new IssueTypeName("Task"),
            new IssueSummary("Summary"),
            new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero),
            [],
            new DateTimeOffset(2026, 7, 1, 11, 0, 0, TimeSpan.Zero));
}
