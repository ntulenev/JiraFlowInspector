using FluentAssertions;

using JiraMetrics.API.Mapping;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.API.Mapping;

public sealed class IssueTimelineMapperTests
{
    [Fact(DisplayName = "Map uses the shared report timestamp for an unresolved issue")]
    [Trait("Category", "Unit")]
    public void MapWhenResolutionDateIsMissingUsesReportTimestamp()
    {
        var generatedAt = new DateTimeOffset(2026, 7, 15, 12, 30, 0, TimeSpan.FromHours(2));
        var mapper = CreateMapper(generatedAt);
        var response = new JiraIssueResponse
        {
            Key = "APP-1",
            Fields = new JiraIssueFieldsResponse
            {
                Created = "2026-07-01T10:00:00Z",
                Summary = "Open issue"
            }
        };

        var timeline = mapper.Map(response, new IssueKey("APP-1"));

        timeline.EndTime.Should().Be(generatedAt);
    }

    [Fact(DisplayName = "Map parses Jira timestamps with their source offsets")]
    [Trait("Category", "Unit")]
    public void MapWhenTimestampsContainOffsetsPreservesTheirInstants()
    {
        var mapper = CreateMapper(new DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.Zero));
        var response = new JiraIssueResponse
        {
            Key = "APP-1",
            Fields = new JiraIssueFieldsResponse
            {
                Created = "2026-07-01T10:00:00+03:00",
                ResolutionDate = "2026-07-02T11:30:00+03:00",
                Summary = "Resolved issue"
            },
            Changelog = new JiraChangelogResponse
            {
                Histories =
                [
                    new JiraHistoryResponse
                    {
                        Created = "2026-07-01T12:00:00+03:00",
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
        };

        var timeline = mapper.Map(response, new IssueKey("APP-1"));

        timeline.Created.Should().Be(new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.FromHours(3)));
        timeline.EndTime.Should().Be(new DateTimeOffset(2026, 7, 2, 11, 30, 0, TimeSpan.FromHours(3)));
        timeline.Transitions.Should().ContainSingle().Which.At.Should()
            .Be(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.FromHours(3)));
    }

    private static IssueTimelineMapper CreateMapper(DateTimeOffset generatedAt)
    {
        var settings = Options.Create(new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.test"),
            new JiraApiToken("token"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            rejectStatusName: null,
            requiredPathStages: [],
            monthLabel: new MonthLabel("2026-07")));

        return new IssueTimelineMapper(
            new TransitionBuilder(settings),
            settings,
            new ReportRunContext(generatedAt));
    }
}
