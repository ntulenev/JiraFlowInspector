using System.Net;

using FluentAssertions;

using JiraMetrics.Models.Configuration;
using JiraMetrics.Transport;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraRetryPolicyTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<JiraOptions> options = null!;

        // Act
        Action act = () => _ = new JiraRetryPolicy(options);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "TryGetDelay returns false when retry attempt is out of range")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenRetryAttemptIsOutOfRangeReturnsFalse()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var resultZero = policy.TryGetDelay(0, HttpStatusCode.ServiceUnavailable, null, null, out _);
        var resultTooHigh = policy.TryGetDelay(2, HttpStatusCode.ServiceUnavailable, null, null, out _);

        // Assert
        resultZero.Should().BeFalse();
        resultTooHigh.Should().BeFalse();
    }

    [Fact(DisplayName = "TryGetDelay retries on transient status codes")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenStatusIsRetryableReturnsTrue()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, HttpStatusCode.TooManyRequests, null, null, out var delay);

        // Assert
        result.Should().BeTrue();
        delay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(200));
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(300));
    }

    [Fact(DisplayName = "TryGetDelay retries on HttpRequestException")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenExceptionIsHttpRequestExceptionReturnsTrue()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, null, null, new HttpRequestException("boom"), out var delay);

        // Assert
        result.Should().BeTrue();
        delay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(200));
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(300));

    }

    [Fact(DisplayName = "TryGetDelay honors server retry delay")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenServerDelayExistsUsesIt()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));
        var serverDelay = TimeSpan.FromSeconds(15);

        // Act
        var result = policy.TryGetDelay(
            1,
            HttpStatusCode.TooManyRequests,
            serverDelay,
            null,
            out var delay);

        // Assert
        result.Should().BeTrue();
        delay.Should().Be(serverDelay);
    }

    [Fact(DisplayName = "TryGetDelay increases exponential backoff")]
    [Trait("Category", "Unit")]
    public void TryGetDelayForLaterAttemptIncreasesBackoff()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 2)));

        // Act
        _ = policy.TryGetDelay(1, HttpStatusCode.ServiceUnavailable, null, null, out var firstDelay);
        _ = policy.TryGetDelay(2, HttpStatusCode.ServiceUnavailable, null, null, out var secondDelay);

        // Assert
        firstDelay.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(300));
        secondDelay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(400));
    }

    [Fact(DisplayName = "TryGetDelay returns false for non-retryable status codes")]
    [Trait("Category", "Unit")]
    public void TryGetDelayWhenStatusIsNotRetryableReturnsFalse()
    {
        // Arrange
        var policy = new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1)));

        // Act
        var result = policy.TryGetDelay(1, HttpStatusCode.BadRequest, null, null, out _);

        // Assert
        result.Should().BeFalse();
    }

    private static JiraOptions CreateOptions(int retryCount)
    {
        return new JiraOptions
        {
            BaseUrl = new Uri("https://example.test/", UriKind.Absolute),
            Email = "user@example.test",
            ApiToken = "token",
            TeamTasks = new TeamTasksOptions
            {
                ProjectKey = "AAA",
                DoneStatusName = "Done",
                IssueTransitions = new IssueTransitionsOptions
                {
                    RequiredPathStages = ["Code Review"]
                }
            },
            MonthLabel = "2026-02",
            RetryCount = retryCount
        };
    }
}
