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
        var serializer = new SimpleJsonSerializer();

        // Act
        Action act = () => _ = new JiraTransport(http, retryPolicy, serializer, new JiraRequestTelemetryCollector());

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
        var serializer = new SimpleJsonSerializer();

        // Act
        Action act = () => _ = new JiraTransport(http, retryPolicy, serializer, new JiraRequestTelemetryCollector());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when serializer is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSerializerIsNullThrowsArgumentNullException()
    {
        // Arrange
        using var http = new HttpClient();
        var retryPolicy = new JiraRetryPolicy(Options.Create(CreateOptions()));
        ISerializer serializer = null!;

        // Act
        Action act = () => _ = new JiraTransport(http, retryPolicy, serializer, new JiraRequestTelemetryCollector());

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "GetAsync throws when url is null")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenUrlIsNullThrowsArgumentNullException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var http = new HttpClient { BaseAddress = new Uri("https://example.test/") };
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());
        Uri url = null!;

        // Act
        Func<Task> act = () => transport.GetAsync<JiraCurrentUserResponse>(url, cts.Token);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
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
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Jane Doe");
    }

    [Fact(DisplayName = "GetAsync records telemetry summary for completed request")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncWhenResponseIsValidRecordsTelemetry()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");
        var telemetryCollector = new JiraRequestTelemetryCollector();

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new JiraCurrentUserResponse { DisplayName = "Jane Doe" }),
                Encoding.UTF8,
                "application/json")
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
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            telemetryCollector);

        // Act
        _ = await transport.GetAsync<JiraCurrentUserResponse>(
            new Uri("rest/api/3/myself", UriKind.Relative),
            cts.Token);
        var summary = telemetryCollector.GetSummary();

        // Assert
        summary.RequestCount.Should().Be(1);
        summary.RetryCount.Should().Be(0);
        summary.Endpoints.Should().ContainSingle();
        summary.Endpoints[0].Method.Should().Be("GET");
        summary.Endpoints[0].Endpoint.Should().Be("rest/api/3/myself");
    }

    [Fact(DisplayName = "PostAsync sends JSON payload and returns deserialized DTO")]
    [Trait("Category", "Unit")]
    public async Task PostAsyncWhenResponseIsValidReturnsDto()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var sendCalls = 0;
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/issue/bulkfetch");
        var requestBody = new { issueIdsOrKeys = new[] { "AAA-1" } };

        var dto = new JiraBulkIssueFetchResponse
        {
            Issues =
            [
                new JiraIssueResponse
                {
                    Id = "10001",
                    Key = "AAA-1"
                }
            ]
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
                    req.Method == HttpMethod.Post
                    && req.RequestUri == requestUrl
                    && req.Content != null
                    && req.Content.Headers.ContentType!.MediaType == "application/json"),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => sendCalls++)
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());

        // Act
        var result = await transport.PostAsync<object, JiraBulkIssueFetchResponse>(
            new Uri("rest/api/3/issue/bulkfetch", UriKind.Relative),
            requestBody,
            cts.Token);

        // Assert
        sendCalls.Should().Be(1);
        result.Should().NotBeNull();
        result!.Issues.Should().ContainSingle();
        result.Issues[0].Key.Should().Be("AAA-1");
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
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());

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
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());

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
            .ReturnsAsync(responses.Dequeue);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions(retryCount: 1))),
            new SimpleJsonSerializer(),
            new JiraRequestTelemetryCollector());

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(new Uri("rest/api/3/myself", UriKind.Relative), cts.Token);

        // Assert
        sendCalls.Should().Be(2);
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "GetAsync uses injected serializer")]
    [Trait("Category", "Unit")]
    public async Task GetAsyncUsesInjectedSerializer()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var baseUri = new Uri("https://example.test/");
        var requestUrl = new Uri(baseUri, "rest/api/3/myself");
        var json = /*lang=json,strict*/ "{\"displayName\":\"Injected\"}";

        var expected = new JiraCurrentUserResponse { DisplayName = "FromSerializer" };
        var serializer = new Mock<ISerializer>(MockBehavior.Strict);
        serializer
            .Setup(s => s.Deserialize<JiraCurrentUserResponse>(It.IsAny<string>()))
            .Returns(expected);

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
            .ReturnsAsync(response);

        using var http = new HttpClient(handler.Object) { BaseAddress = baseUri };
        var transport = new JiraTransport(
            http,
            new JiraRetryPolicy(Options.Create(CreateOptions())),
            serializer.Object,
            new JiraRequestTelemetryCollector());

        // Act
        var result = await transport.GetAsync<JiraCurrentUserResponse>(
            new Uri("rest/api/3/myself", UriKind.Relative),
            cts.Token);

        // Assert
        result.Should().BeSameAs(expected);
        serializer.Verify(s => s.Deserialize<JiraCurrentUserResponse>(json), Times.Once);
    }

    private static JiraOptions CreateOptions(int retryCount = 0)
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
