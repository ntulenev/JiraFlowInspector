using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Transport;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

namespace JiraMetrics.Tests.Transport;

public sealed class JiraTransportTests
{
    [Fact(DisplayName = "Constructor throws when http client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHttpClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        HttpClient http = null!;
        var retryPolicy = new JiraRetryPolicy(Options.Create(CreateOptions()));

        // Act
        Action act = () => _ = new JiraTransport(http, retryPolicy);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when retry policy is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRetryPolicyIsNullThrowsArgumentNullException()
    {
        // Arrange
        using var http = new HttpClient();
        IJiraRetryPolicy retryPolicy = null!;

        // Act
        Action act = () => _ = new JiraTransport(http, retryPolicy);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetAsync returns deserialized DTO when response is valid")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsValidReturnsDto()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");

        var dto = new JiraCurrentUserResponse
        {
            DisplayName = "Jane Doe",
            EmailAddress = "user@example.test",
            AccountId = "123"
        };
        var json = JsonSerializer.Serialize(dto);

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(http, new JiraRetryPolicy(Options.Create(CreateOptions())));

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Jane Doe");
    }

    [Fact(DisplayName = "GetAsync returns null when response body is null")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseBodyIsNullReturnsNull()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(http, new JiraRetryPolicy(Options.Create(CreateOptions())));

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().BeNull();
    }

    [Fact(DisplayName = "GetAsync throws when response is not successful")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsFailureThrowsHttpRequestException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");
        var body = "error body";

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent(body, Encoding.UTF8, "text/plain")
        };

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(http, new JiraRetryPolicy(Options.Create(CreateOptions())));

        // Act
        Func<Task> act = () => transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<HttpRequestException>();

        sendCalls.Should().Be(1);
    }

    [Fact(DisplayName = "GetAsync retries transient failures and succeeds")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenTransientFailureRetriesAndSucceeds()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");

        using var firstResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable",
            Content = new StringContent("temporary", Encoding.UTF8, "text/plain")
        };

        var dto = new JiraCurrentUserResponse { DisplayName = "Jane Doe" };
        var json = JsonSerializer.Serialize(dto);

        using var secondResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var responses = new Queue<HttpResponseMessage>([firstResponse, secondResponse]);

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && req.RequestUri == requestUrl),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(() => responses.Dequeue());

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(http, new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1))));

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        result.Should().NotBeNull();
    }

    private static JiraOptions CreateOptions(int retryCount = 0)
    {
        return new JiraOptions
        {
            BaseUrl = new Uri("https://example.test/", UriKind.Absolute),
            Email = "user@example.test",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            MonthLabel = "2026-02",
            RetryCount = retryCount
        };
    }
}
