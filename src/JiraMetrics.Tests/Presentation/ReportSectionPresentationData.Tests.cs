using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
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

    [Fact(DisplayName = "Ratio presentation data orders issues and calculates shared metrics")]
    public void IssueRatioPresentationDataCreateOrdersIssuesAndCalculatesMetrics()
    {
        var duplicateProdIssue = CreateIssue("APP-2", reproducedOnProd: true);
        var openNonProdIssue = CreateIssue("APP-1", reproducedOnProd: false);
        var rejectedProdIssue = CreateIssue("APP-3", reproducedOnProd: true);
        var snapshot = new IssueRatioSnapshot(
            new ItemCount(4),
            new ItemCount(2),
            new ItemCount(1),
            new ItemCount(1),
            new ItemCount(2),
            [duplicateProdIssue, openNonProdIssue],
            [duplicateProdIssue],
            [rejectedProdIssue]);

        var result = IssueRatioPresentationData.Create(snapshot);

        result.OpenIssues.Select(static issue => issue.Key.Value)
            .Should().Equal("APP-1", "APP-2");
        result.DoneIssues.Select(static issue => issue.Key.Value)
            .Should().Equal("APP-2");
        result.RejectedIssues.Select(static issue => issue.Key.Value)
            .Should().Equal("APP-3");
        result.ReproducedOnProdCount.Value.Should().Be(2);
        result.OpenReproducedOnProdCount.Value.Should().Be(1);
        result.DoneReproducedOnProdCount.Value.Should().Be(1);
        result.RejectedReproducedOnProdCount.Value.Should().Be(1);
        result.FinishedReproducedOnProdCount.Value.Should().Be(2);
        result.FinishedToCreatedRatioText.Should().Be("50%");
        result.HasIssueDetails.Should().BeTrue();
    }

    [Fact(DisplayName = "Test coverage presentation data prepares shared labels and percentage")]
    public void TestCoveragePresentationDataCreatePreparesLabelsAndPercentage()
    {
        var settings = new TestCoverageSettings(
            issueTypes: [new IssueTypeName("Story"), new IssueTypeName("Task")],
            testProjectKey: new ProjectKey("TEST"),
            linkName: "is verified by");
        var firstIssue = CreateIssue("APP-1", reproducedOnProd: false);
        var secondIssue = CreateIssue("APP-2", reproducedOnProd: false);

        var result = TestCoveragePresentationData.Create(
            settings,
            new TestCoverageSnapshot([firstIssue, secondIssue], [firstIssue]));

        result.IssueTypesLabel.Should().Be("Story, Task");
        result.TestProjectLabel.Should().Be("TEST");
        result.LinkLabel.Should().Be("is verified by");
        result.TotalIssues.Value.Should().Be(2);
        result.CoveredIssueCount.Value.Should().Be(1);
        result.CoveragePercentage.Should().Be(50);
        result.CoverageText.Should().Be("50%");
    }

    [Fact(DisplayName = "Ratio section presentation data applies optional section visibility")]
    public void RatioSectionPresentationDataCreateAppliesOptionalSectionVisibility()
    {
        var ratio = new IssueRatioSnapshot(
            new ItemCount(1),
            new ItemCount(1),
            new ItemCount(0),
            new ItemCount(0),
            new ItemCount(0),
            [],
            [],
            []);
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("APP"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-07"),
            bugIssueNames: [new IssueTypeName("Bug")],
            testCoverage: new TestCoverageSettings(enabled: false));
        var reportData = new JiraReportData
        {
            RunContext = ReportRunContext.Create(TimeProvider.System),
            Settings = settings,
            Ratios = new JiraReportRatioData
            {
                AllTasks = ratio,
                Bugs = ratio,
                InternalIncidents = ratio
            }
        };

        var result = RatioSectionPresentationData.Create(reportData);

        result.AllTasks.Should().NotBeNull();
        result.Bugs.Should().NotBeNull();
        result.BugIssueTypesLabel.Should().Be("Bug");
        result.InternalIncidents.Should().BeNull();
        result.TestCoverage.Should().BeNull();
    }

    private static IssueListItem CreateIssue(string key, bool reproducedOnProd) =>
        new(
            new IssueKey(key),
            new IssueSummary($"Issue {key}"),
            reporducedOnProd: reproducedOnProd);
}
