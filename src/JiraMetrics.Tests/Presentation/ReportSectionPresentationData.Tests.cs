using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

namespace JiraMetrics.Tests.Presentation;

public sealed class ReportSectionPresentationDataTests
{
    [Fact(DisplayName = "Release presentation data orders rows and builds shared totals")]
    public void ReleasePresentationDataCreateOrdersRowsAndBuildsTotals()
    {
        var releases = new[]
        {
            new ReleaseIssueItem(
                new IssueKey("RLS-2"),
                new IssueSummary("Second"),
                new DateOnly(2026, 7, 2),
                componentNames: ["api"],
                isHotFix: true),
            new ReleaseIssueItem(
                new IssueKey("RLS-1"),
                new IssueSummary("First"),
                new DateOnly(2026, 7, 1),
                componentNames: ["API", "Web"],
                rollbackType: "Full")
        };

        var result = ReleasePresentationData.Create(releases);

        result.Releases.Select(static release => release.Key.Value)
            .Should().Equal("RLS-1", "RLS-2");
        result.TotalCount.Value.Should().Be(2);
        result.HotFixCount.Value.Should().Be(1);
        result.RollbackCount.Value.Should().Be(1);
        result.Components.Select(static component => (
                component.ComponentName,
                component.ReleaseCount.Value))
            .Should().Equal(("API", 2), ("Web", 1));
    }

    [Fact(DisplayName = "Global incident presentation data orders rows and sums valid durations")]
    public void GlobalIncidentsPresentationDataCreateOrdersRowsAndSumsDurations()
    {
        var firstStart = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var secondStart = firstStart.AddDays(1);
        var incidents = new[]
        {
            new GlobalIncidentItem(
                new IssueKey("INC-2"),
                new IssueSummary("Second"),
                secondStart,
                secondStart.AddMinutes(45)),
            new GlobalIncidentItem(
                new IssueKey("INC-1"),
                new IssueSummary("First"),
                firstStart,
                firstStart.AddMinutes(15)),
            new GlobalIncidentItem(
                new IssueKey("INC-3"),
                new IssueSummary("Unknown start"),
                null,
                null)
        };

        var result = GlobalIncidentsPresentationData.Create(incidents);

        result.Incidents.Select(static incident => incident.Key.Value)
            .Should().Equal("INC-3", "INC-1", "INC-2");
        result.TotalDuration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact(DisplayName = "Empty presentation data has zero totals")]
    public void CreateWithEmptyCollectionsReturnsEmptyPresentationData()
    {
        var releases = ReleasePresentationData.Create([]);
        var incidents = GlobalIncidentsPresentationData.Create([]);

        releases.Releases.Should().BeEmpty();
        releases.Components.Should().BeEmpty();
        releases.TotalCount.Value.Should().Be(0);
        releases.HotFixCount.Value.Should().Be(0);
        releases.RollbackCount.Value.Should().Be(0);
        incidents.Incidents.Should().BeEmpty();
        incidents.TotalDuration.Should().BeNull();
    }
}
