using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraIssueTimelineClientBehaviorTests
{
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
                cts.Token))
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
    private static JiraIssueTimelineClient CreateClient(
        IJiraTransport transport,
        IOptions<AppSettings>? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        return new JiraIssueTimelineClient(
            new JiraSearchExecutor(transport),
            resolvedSettings,
            new JiraFieldResolver(transport),
            new IssueTimelineMapper(CreateTransitionBuilder(resolvedSettings), resolvedSettings));
    }

    private static TransitionBuilder CreateTransitionBuilder(IOptions<AppSettings> settings) => new(settings);

    private static readonly string[] _bulkTimelineIssueKeys = ["AAA-1", "AAA-2"];
}
