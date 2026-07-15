using FluentAssertions;

using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Moq;

namespace JiraMetrics.Tests.API.FieldResolution;

public sealed class JiraFieldResolverTests
{
    [Fact(DisplayName = "Resolver returns null for absent optional field without loading metadata")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenFieldNameIsNullReturnsNull()
    {
        // Arrange
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var result = await resolver.TryResolveFieldIdAsync(null, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        transport.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Resolver matches field id before display names")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenConfiguredValueIsIdReturnsIdMatch()
    {
        // Arrange
        var transport = CreateTransport(
            new JiraFieldResponse { Id = "customfield_10001", Name = "Other" },
            new JiraFieldResponse { Id = "different", Name = "customfield_10001" });
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var result = await resolver.TryResolveFieldIdAsync(
            new JiraFieldName("CUSTOMFIELD_10001"),
            CancellationToken.None);

        // Assert
        result.Should().Be(new JiraFieldId("customfield_10001"));
    }

    [Fact(DisplayName = "Resolver prefers system field for duplicate exact names")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenExactNamesAreDuplicatedPrefersSystemField()
    {
        // Arrange
        var transport = CreateTransport(
            new JiraFieldResponse { Id = "customfield_10002", Name = "Environment" },
            new JiraFieldResponse { Id = "environment", Name = " Environment " },
            new JiraFieldResponse { Id = "customfield_10001", Name = "Environment" });
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var result = await resolver.TryResolveFieldIdAsync(
            new JiraFieldName("environment"),
            CancellationToken.None);

        // Assert
        result.Should().Be(new JiraFieldId("environment"));
    }

    [Fact(DisplayName = "Resolver matches normalized display name")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenFormattingDiffersReturnsNormalizedNameMatch()
    {
        // Arrange
        var transport = CreateTransport(
            new JiraFieldResponse { Id = "customfield_10001", Name = "Incident Start date/time UTC" });
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var result = await resolver.TryResolveFieldIdAsync(
            new JiraFieldName("incident-start date time (UTC)"),
            CancellationToken.None);

        // Assert
        result.Should().Be(new JiraFieldId("customfield_10001"));
    }

    [Fact(DisplayName = "Resolver omits fallback date field resolved to primary id")]
    [Trait("Category", "Unit")]
    public async Task ResolveDateFieldsAsyncWhenFallbackHasSameIdReturnsPrimaryOnly()
    {
        // Arrange
        var transport = CreateTransport(
            new JiraFieldResponse { Id = "customfield_10001", Name = "Start UTC" });
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var result = await resolver.ResolveDateFieldsAsync(
            new JiraFieldName("Start UTC"),
            new JiraFieldName("customfield_10001"),
            CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].FieldName.Should().Be(new JiraFieldName("Start UTC"));
        result[0].FieldId.Should().Be(new JiraFieldId("customfield_10001"));
    }

    [Fact(DisplayName = "Resolver caches loaded metadata across field lookups")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenCalledForMultipleFieldsLoadsMetadataOnce()
    {
        // Arrange
        var transport = CreateTransport(
            new JiraFieldResponse { Id = "customfield_10001", Name = "First" },
            new JiraFieldResponse { Id = "customfield_10002", Name = "Second" });
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var first = await resolver.TryResolveFieldIdAsync(new JiraFieldName("First"), CancellationToken.None);
        var second = await resolver.TryResolveFieldIdAsync(new JiraFieldName("Second"), CancellationToken.None);
        var cachedFirst = await resolver.TryResolveFieldIdAsync(new JiraFieldName("FIRST"), CancellationToken.None);

        // Assert
        first.Should().Be(new JiraFieldId("customfield_10001"));
        second.Should().Be(new JiraFieldId("customfield_10002"));
        cachedFirst.Should().Be(first);
        VerifyMetadataRequestCount(transport, 1);
    }

    [Fact(DisplayName = "Resolver retries metadata loading after transient failure")]
    [Trait("Category", "Unit")]
    public async Task TryResolveFieldIdAsyncWhenMetadataLoadFailsAllowsNextCallToRetry()
    {
        // Arrange
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport.SetupSequence(t => t.GetAsync<List<JiraFieldResponse>>(
                MetadataUri,
                CancellationToken.None))
            .ThrowsAsync(new HttpRequestException("temporary"))
            .ReturnsAsync([new JiraFieldResponse { Id = "customfield_10001", Name = "Environment" }]);
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        var secondResult = await InvokeAfterFailureAsync(FirstCall, resolver);

        // Assert
        secondResult.Should().Be(new JiraFieldId("customfield_10001"));
        VerifyMetadataRequestCount(transport, 2);

        Task<JiraFieldId?> FirstCall()
        {
            return resolver.TryResolveFieldIdAsync(
                new JiraFieldName("Environment"),
                CancellationToken.None);
        }
    }

    [Fact(DisplayName = "Resolver rejects empty Jira field response")]
    [Trait("Category", "Unit")]
    public async Task ResolveFieldIdAsyncWhenMetadataResponseIsNullThrowsInvalidOperationException()
    {
        // Arrange
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport.Setup(t => t.GetAsync<List<JiraFieldResponse>>(MetadataUri, CancellationToken.None))
            .ReturnsAsync((List<JiraFieldResponse>?)null);
        var resolver = new JiraFieldResolver(transport.Object);

        // Act
        // Assert
        await FluentActions.Awaiting(Act).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Jira fields response is empty.");

        Task<JiraFieldId> Act()
        {
            return resolver.ResolveFieldIdAsync(
                new JiraFieldName("Environment"),
                CancellationToken.None);
        }
    }

    private static async Task<JiraFieldId?> InvokeAfterFailureAsync(
        Func<Task<JiraFieldId?>> firstCall,
        JiraFieldResolver resolver)
    {
        await firstCall.Should().ThrowAsync<HttpRequestException>();
        return await resolver.TryResolveFieldIdAsync(
            new JiraFieldName("Environment"),
            CancellationToken.None);
    }

    private static Mock<IJiraTransport> CreateTransport(params JiraFieldResponse[] fields)
    {
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport.Setup(t => t.GetAsync<List<JiraFieldResponse>>(MetadataUri, CancellationToken.None))
            .ReturnsAsync([.. fields]);
        return transport;
    }

    private static void VerifyMetadataRequestCount(Mock<IJiraTransport> transport, int count) =>
        transport.Verify(
            t => t.GetAsync<List<JiraFieldResponse>>(MetadataUri, CancellationToken.None),
            Times.Exactly(count));

    private static Uri MetadataUri { get; } = new("rest/api/3/field", UriKind.Relative);
}
