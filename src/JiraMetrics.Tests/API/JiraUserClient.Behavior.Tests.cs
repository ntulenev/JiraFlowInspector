using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.Search;
using JiraMetrics.Transport.Models;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraUserClientBehaviorTests
{
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
                cts.Token))
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
                cts.Token))
            .ReturnsAsync((JiraCurrentUserResponse?)null);

        var client = CreateClient(transport.Object);

        // Act
        Func<Task> act = () => client.GetCurrentUserAsync(cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();
    }
    private static JiraUserClient CreateClient(IJiraTransport transport) =>
        new(new JiraSearchExecutor(transport));
}
