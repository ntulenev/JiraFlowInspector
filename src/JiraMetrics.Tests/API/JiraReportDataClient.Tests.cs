using System.Text.Json;

using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraReportDataClientTests
{
    [Fact(DisplayName = "GetArchTasksAsync returns created and resolved timestamps")]
    [Trait("Category", "Unit")]
    public async Task GetArchTasksAsyncWhenCalledReturnsArchitectureTasks()
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
                    Key = "AAA-7",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Architecture review",
                        Created = "2026-03-02T09:30:00Z",
                        ResolutionDate = "2026-03-05T12:00:00Z"
                    }
                }
            ],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-03");
        var client = CreateClient(transport.Object, settings);

        // Act
        var tasks = await client.GetArchTasksAsync(
            new ArchTasksReportSettings(
                "project = AAA AND type = \"Arch Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC"),
            cts.Token);

        // Assert
        tasks.Should().ContainSingle();
        tasks[0].Key.Value.Should().Be("AAA-7");
        tasks[0].Title.Value.Should().Be("Architecture review");
        tasks[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 3, 2, 9, 30, 0, TimeSpan.Zero));
        tasks[0].ResolvedAt.Should().Be(new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero));
        capturedUrl.Should().Contain("resolved");
        capturedUrl.Should().Contain("2026-03-01");
        capturedUrl.Should().Contain("2026-04-01");
        capturedUrl.Should().Contain("fields=key,summary,created,resolutiondate");
    }

    [Fact(DisplayName = "GetUnresolved30DaysTasksAsync uses configured JQL and returns task details")]
    [Trait("Category", "Unit")]
    public async Task GetUnresolved30DaysTasksAsyncWhenCalledReturnsTasks()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedUrl = string.Empty;
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(It.IsAny<Uri>(), cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(new JiraSearchResponse
            {
                Issues =
                [
                    new JiraIssueKeyResponse
                    {
                        Key = "AAA-30",
                        Fields = new JiraIssueFieldsResponse
                        {
                            Summary = "Long-running task",
                            Created = "2026-01-01T09:30:00Z",
                            IssueType = new JiraIssueTypeResponse { Name = "Story" },
                            Assignee = new JiraUserResponse { DisplayName = "Ada Lovelace" },
                            Status = new JiraIssueStatusResponse { Name = "In Progress" }
                        }
                    }
                ],
                IsLast = true
            });
        var client = CreateClient(transport.Object);
        const string jql = "project = AAA AND statusCategory != Done AND created <= -30d ORDER BY created ASC";

        // Act
        var tasks = await client.GetUnresolved30DaysTasksAsync(
            new Unresolved30DaysTasksReportSettings(jql),
            cts.Token);

        // Assert
        tasks.Should().ContainSingle();
        tasks[0].Key.Value.Should().Be("AAA-30");
        tasks[0].Title.Value.Should().Be("Long-running task");
        tasks[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 1, 1, 9, 30, 0, TimeSpan.Zero));
        tasks[0].IssueType.Should().Be("Story");
        tasks[0].Assignee.Should().Be("Ada Lovelace");
        tasks[0].Status.Should().Be("In Progress");
        capturedUrl.Should().Contain("created%20%3C%3D%20-30d");
        capturedUrl.Should().Contain("fields=key,summary,created,issuetype,assignee,status");
    }

    [Fact(DisplayName = "GetRoadmapItemsAsync reads dates from a Jira interval field")]
    [Trait("Category", "Unit")]
    public async Task GetRoadmapItemsAsyncWhenCalledReturnsRoadmapItems()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        using var roadmapJson = JsonDocument.Parse("{\"value\":\"Committed\"}");
        using var intervalJson = JsonDocument.Parse("\"{\\\"start\\\":\\\"2026-02-01\\\",\\\"end\\\":\\\"2026-04-30\\\"}\"");
        var capturedUrl = string.Empty;
        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(It.IsAny<Uri>(), cts.Token))
            .ReturnsAsync(
            [
                new JiraFieldResponse { Id = "customfield_10001", Name = "Roadmap[Dropdown]" }
            ]);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(It.IsAny<Uri>(), cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedUrl = url.ToString())
            .ReturnsAsync(new JiraSearchResponse
            {
                Issues =
                [
                    new JiraIssueKeyResponse
                    {
                        Key = "PLAN-1",
                        Fields = new JiraIssueFieldsResponse
                        {
                            Summary = "Platform Growth",
                            Status = new JiraIssueStatusResponse { Name = "In Progress" },
                            AdditionalFields = new Dictionary<string, JsonElement>
                            {
                                ["customfield_10001"] = roadmapJson.RootElement.Clone(),
                                ["customfield_15928"] = intervalJson.RootElement.Clone()
                            }
                        }
                    }
                ],
                IsLast = true
            });
        var client = CreateClient(transport.Object);

        // Act
        var items = await client.GetRoadmapItemsAsync(
            new RoadmapReportSettings(
                "project = PROJECT_KEY AND issuetype = IDEA_TYPE",
                "Roadmap[Dropdown]",
                "cf[15928][startDate]",
                "cf[15928][endDate]"),
            cts.Token);

        // Assert
        items.Should().ContainSingle();
        items[0].Should().Be(new RoadmapItem(
            new IssueKey("PLAN-1"),
            new IssueSummary("Platform Growth"),
            "In Progress",
            "Committed",
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 4, 30)));
        capturedUrl.Should().Contain("project%20%3D%20PROJECT_KEY");
        capturedUrl.Should().Contain("fields=key,summary,status,customfield_10001,customfield_15928");
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
            },
            new()
            {
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var environmentJson = JsonDocument.Parse("[{\"value\":\"P005\"}]");
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
                        Status = new JiraIssueStatusResponse { Name = "Ready for Prod" },
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
                                    Key = "NOVA-100"
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
                                    Key = "NOVA-101"
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
                                    Key = "NOVA-999"
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(),
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-1");
        releases[0].Title.Value.Should().Be("Release item");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Status.Value.Should().Be("Ready for Prod");
        releases[0].Tasks.Should().Be(3);
        releases[0].ComponentNames.Should().BeEmpty();
        releases[0].EnvironmentNames.Should().BeEmpty();
        releases[0].IsHotFix.Should().BeFalse();
        capturedSearchUrl.Should().Contain("project");
        capturedSearchUrl.Should().Contain("RLS");
        capturedSearchUrl.Should().Contain("labels");
        capturedSearchUrl.Should().Contain("Processing");
        capturedSearchUrl.Should().Contain("Change%20completion%20date");
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("status");
        capturedSearchUrl.Should().Contain("issuelinks");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync adds environment filter when configured")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenEnvironmentFilterIsConfiguredAddsClause()
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
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var environmentJson = JsonDocument.Parse("[{\"value\":\"P005\"}]");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-7",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "P005 release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_10865"] = environmentJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(
                environmentFieldName: "customfield_10865",
                environmentFieldValue: "P005"),
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-7");
        releases[0].EnvironmentNames.Should().ContainSingle().Which.Should().Be("P005");
        capturedSearchUrl.Should().Contain("customfield_10865");
        capturedSearchUrl.Should().Contain("P005");
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(),
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-2");
        releases[0].ReleaseDate.Should().Be(new DateOnly(2026, 2, 14));
        releases[0].Status.Should().Be(StatusName.Unknown);
        releases[0].Tasks.Should().Be(0);
        releases[0].ComponentNames.Should().BeEmpty();
        releases[0].EnvironmentNames.Should().BeEmpty();
        releases[0].IsHotFix.Should().BeFalse();
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
            },
            new()
            {
                Id = "customfield_10865",
                Name = "Environment"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-14\"");
        using var componentsJson = JsonDocument.Parse("[{\"value\":\"Nebula PostgreSQL Database\"},{\"value\":\"Flux\"}]");
        using var environmentsJson = JsonDocument.Parse("[{\"value\":\"P005\"},{\"value\":\"S005\"}]");
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
                        Status = new JiraIssueStatusResponse { Name = "Released" },
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_77777"] = componentsJson.RootElement.Clone(),
                            ["customfield_10865"] = environmentsJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(
                componentsFieldName: "Components",
                environmentFieldName: "customfield_10865",
                environmentFieldValue: "P005"),
            cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-3");
        releases[0].Status.Value.Should().Be("Released");
        releases[0].Components.Should().Be(2);
        releases[0].ComponentNames.Should().ContainInOrder("Flux", "Nebula PostgreSQL Database");
        releases[0].EnvironmentNames.Should().ContainInOrder("P005", "S005");
        releases[0].IsHotFix.Should().BeFalse();
        capturedSearchUrl.Should().Contain("customfield_12345");
        capturedSearchUrl.Should().Contain("customfield_77777");
        capturedSearchUrl.Should().Contain("customfield_10865");
        capturedSearchUrl.Should().Contain("status");
        capturedSearchUrl.Should().Contain("components");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync maps rollback payload when rollback field contains value")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenRollbackFieldContainsValueMapsRollbackPayload()
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
                Id = "customfield_66666",
                Name = "Rollback type"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-17\"");
        using var rollbackJson = JsonDocument.Parse("{\"value\":\"Full rollback\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-6",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Rollback release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_66666"] = rollbackJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(),
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-6");
        releases[0].RollbackType.Should().Be("Full rollback");
        capturedSearchUrl.Should().Contain("customfield_66666");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync marks release as hot-fix when configured field matches value")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenHotFixFieldMatchesMarksReleaseAsHotFix()
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
                Id = "customfield_88888",
                Name = "Change type"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-16\"");
        using var hotFixJson = JsonDocument.Parse("{\"value\":\"Emergency\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-4",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Emergency release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_88888"] = hotFixJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(),
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-4");
        releases[0].IsHotFix.Should().BeTrue();
        capturedSearchUrl.Should().Contain("customfield_88888");
    }

    [Fact(DisplayName = "GetReleaseIssuesForMonthAsync marks release as hot-fix when any configured rule matches")]
    [Trait("Category", "Unit")]
    public async Task GetReleaseIssuesForMonthAsyncWhenAnyHotFixRuleMatchesMarksReleaseAsHotFix()
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
                Id = "customfield_88888",
                Name = "Change type"
            },
            new()
            {
                Id = "customfield_99999",
                Name = "Change reason"
            }
        };

        using var releaseDateJson = JsonDocument.Parse("\"2026-02-16\"");
        using var changeReasonJson = JsonDocument.Parse("{\"value\":\"Mitigation\"}");
        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "RLS-5",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "Mitigation release",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_12345"] = releaseDateJson.RootElement.Clone(),
                            ["customfield_99999"] = changeReasonJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var releases = await client.GetReleaseIssuesForMonthAsync(
            CreateReleaseIssueReadRequest(
                hotFixRules: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["Change type"] = ["Emergency"],
                    ["Change reason"] = ["Repair", "Mitigation"]
                }),
            cancellationToken: cts.Token);

        // Assert
        releases.Should().ContainSingle();
        releases[0].Key.Value.Should().Be("RLS-5");
        releases[0].IsHotFix.Should().BeTrue();
        capturedSearchUrl.Should().Contain("customfield_88888");
        capturedSearchUrl.Should().Contain("customfield_99999");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync uses namespace and configured JQL filter")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenJqlFilterIsConfiguredAddsRawClause()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_10802",
                Name = "Impact"
            },
            new()
            {
                Id = "customfield_10847",
                Name = "Urgency"
            },
            new()
            {
                Id = "customfield_11348",
                Name = "Business Impact"
            }
        };

        using var incidentStartJson = JsonDocument.Parse("\"2026-02-12 10:00\"");
        using var incidentRecoveryJson = JsonDocument.Parse("\"2026-02-12 10:49\"");
        using var impactJson = JsonDocument.Parse("{\"value\":\"Significant / Large\"}");
        using var urgencyJson = JsonDocument.Parse("{\"value\":\"High\"}");
        using var businessImpactJson = JsonDocument.Parse("{\"type\":\"doc\",\"version\":1,\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"ORX live feed unavailable\"}]}]}");

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "INC-11861",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "NOVA - ORX disabled 10/03/2026",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_14851"] = incidentStartJson.RootElement.Clone(),
                            ["customfield_14852"] = incidentRecoveryJson.RootElement.Clone(),
                            ["customfield_10802"] = impactJson.RootElement.Clone(),
                            ["customfield_10847"] = urgencyJson.RootElement.Clone(),
                            ["customfield_11348"] = businessImpactJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        var incidents = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(
                namespaceName: "Incidents",
                jqlFilter: "(\"Incident categorization\" = ORX OR labels = ORX OR summary ~ \"ORX\") AND (summary ~ \"disab*\" OR summary ~ \"unavail*\" OR summary ~ \"downtime\")",
                additionalFieldNames: ["Business Impact"]),
            cts.Token);

        // Assert
        incidents.Should().ContainSingle();
        incidents[0].Key.Value.Should().Be("INC-11861");
        incidents[0].Impact.Should().Be("Significant / Large");
        incidents[0].Urgency.Should().Be("High");
        incidents[0].Duration.Should().Be(TimeSpan.FromMinutes(49));
        incidents[0].AdditionalFields.Should().ContainKey("Business Impact");
        incidents[0].AdditionalFields["Business Impact"].Should().Be("ORX live feed unavailable");
        capturedSearchUrl.Should().Contain("project%20%3D%20%22Incidents%22");
        capturedSearchUrl.Should().Contain("Incident%20categorization");
        capturedSearchUrl.Should().Contain("labels%20%3D%20ORX");
        capturedSearchUrl.Should().Contain("summary%20~%20%22ORX%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22disab%2A%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22unavail%2A%22");
        capturedSearchUrl.Should().Contain("summary%20~%20%22downtime%22");
        capturedSearchUrl.Should().Contain("customfield_14851");
        capturedSearchUrl.Should().Contain("customfield_11348");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync uses fallback start and recovery fields when primary fields are empty")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenPrimaryDateFieldsAreEmptyUsesFallbackFields()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_11416",
                Name = "Incident Start date/time user timezone"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_11417",
                Name = "Incident Recovery date/time user timezone"
            },
            new()
            {
                Id = "customfield_11348",
                Name = "Business Impact"
            }
        };

        using var fallbackStartJson = JsonDocument.Parse("\"2026-01-12T03:52:00.000+0100\"");
        using var fallbackRecoveryJson = JsonDocument.Parse("\"2026-01-12T04:49:00.000+0100\"");
        using var businessImpactJson = JsonDocument.Parse("{\"type\":\"doc\",\"version\":1,\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"ADF services were unavailable due to Google maintenance\"}]}]}");

        var pageResponse = new JiraSearchResponse
        {
            Issues =
            [
                new JiraIssueKeyResponse
                {
                    Key = "INC-11586",
                    Fields = new JiraIssueFieldsResponse
                    {
                        Summary = "SB2 - ADF disablement since services was down 12/01/2025",
                        AdditionalFields = new Dictionary<string, JsonElement>
                        {
                            ["customfield_11416"] = fallbackStartJson.RootElement.Clone(),
                            ["customfield_11417"] = fallbackRecoveryJson.RootElement.Clone(),
                            ["customfield_11348"] = businessImpactJson.RootElement.Clone()
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
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-01");
        var client = CreateClient(transport.Object, settings);

        // Act
        var incidents = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(
                namespaceName: "Incidents",
                jqlFilter: "labels = ADF AND summary ~ \"disab*\"",
                incidentStartFallbackFieldName: "Incident Start date/time user timezone",
                incidentRecoveryFallbackFieldName: "Incident Recovery date/time user timezone",
                additionalFieldNames: ["Business Impact"]),
            cts.Token);

        // Assert
        incidents.Should().ContainSingle();
        incidents[0].Key.Value.Should().Be("INC-11586");
        incidents[0].IncidentStartUtc.Should().Be(new DateTimeOffset(2026, 1, 12, 2, 52, 0, TimeSpan.Zero));
        incidents[0].IncidentRecoveryUtc.Should().Be(new DateTimeOffset(2026, 1, 12, 3, 49, 0, TimeSpan.Zero));
        incidents[0].Duration.Should().Be(TimeSpan.FromMinutes(57));
        capturedSearchUrl.Should().Contain("customfield_14851");
        capturedSearchUrl.Should().Contain("customfield_11416");
        capturedSearchUrl.Should().Contain("customfield_14852");
        capturedSearchUrl.Should().Contain("customfield_11417");
        capturedSearchUrl.Should().Contain("Incident%20Start%20date%2Ftime%20user%20timezone%22%20%3E%3D%20%222026-01-01");
    }

    [Fact(DisplayName = "GetGlobalIncidentsForMonthAsync falls back to search phrase when JQL filter is missing")]
    [Trait("Category", "Unit")]
    public async Task GetGlobalIncidentsForMonthAsyncWhenJqlFilterIsMissingUsesSearchPhraseTerms()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var capturedSearchUrl = string.Empty;

        var fieldResponse = new List<JiraFieldResponse>
        {
            new()
            {
                Id = "customfield_14851",
                Name = "Incident Start date/time UTC"
            },
            new()
            {
                Id = "customfield_14852",
                Name = "Incident Recovery date/time UTC"
            },
            new()
            {
                Id = "customfield_10802",
                Name = "Impact"
            },
            new()
            {
                Id = "customfield_10847",
                Name = "Urgency"
            }
        };

        var pageResponse = new JiraSearchResponse
        {
            Issues = [],
            IsLast = true
        };

        var transport = new Mock<IJiraTransport>(MockBehavior.Strict);
        transport
            .Setup(t => t.GetAsync<List<JiraFieldResponse>>(
                It.Is<Uri>(u => u.ToString().Contains("rest/api/3/field", StringComparison.Ordinal)),
                cts.Token))
            .ReturnsAsync(fieldResponse);
        transport
            .Setup(t => t.GetAsync<JiraSearchResponse>(
                It.IsAny<Uri>(),
                cts.Token))
            .Callback<Uri, CancellationToken>((url, _) => capturedSearchUrl = url.ToString())
            .ReturnsAsync(pageResponse);

        var settings = CreateSettings(monthLabel: "2026-02");
        var client = CreateClient(transport.Object, settings);

        // Act
        _ = await client.GetGlobalIncidentsForMonthAsync(
            new GlobalIncidentsReportSettings(namespaceName: "Incidents", searchPhrase: "ORX disab"),
            cts.Token);

        // Assert
        capturedSearchUrl.Should().Contain("text%20~%20%22ORX%2A%22");
        capturedSearchUrl.Should().Contain("text%20~%20%22disab%2A%22");
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
    private static ReleaseIssueReadRequest CreateReleaseIssueReadRequest(
        string? componentsFieldName = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? hotFixRules = null,
        string rollbackFieldName = "Rollback type",
        string? environmentFieldName = null,
        string? environmentFieldValue = null)
    {
        var resolvedHotFixRules = hotFixRules
            ?? new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"]
            };

        return new ReleaseIssueReadRequest(
            new ProjectKey("RLS"),
            new JiraLabel("Processing"),
            new JiraFieldName("Change completion date"),
            JiraFieldName.FromNullable(componentsFieldName),
            [.. resolvedHotFixRules.Select(static pair => new HotFixRule(
                new JiraFieldName(pair.Key),
                [.. pair.Value.Select(static value => new JiraFieldValue(value))]))],
            new JiraFieldName(rollbackFieldName),
            JiraFieldName.FromNullable(environmentFieldName) is { } resolvedEnvironmentFieldName
                && JiraFieldValue.FromNullable(environmentFieldValue) is { } resolvedEnvironmentFieldValue
                    ? new ReleaseEnvironmentFilter(resolvedEnvironmentFieldName, resolvedEnvironmentFieldValue)
                    : null);
    }
    private static JiraReportDataClient CreateClient(
        IJiraTransport transport,
        IOptions<AppSettings>? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        return new JiraReportDataClient(
            new JiraSearchExecutor(transport),
            new JiraJqlFacade(
                new TeamTasksJqlBuilder(resolvedSettings),
                new ReleaseIssuesJqlBuilder(resolvedSettings),
                new ArchTasksJqlBuilder(resolvedSettings),
                new GlobalIncidentsJqlBuilder(resolvedSettings)),
            new JiraFieldResolver(transport),
            new ReleaseIssueMapper(),
            new GlobalIncidentMapper(),
            new RoadmapItemMapper());
    }
}
