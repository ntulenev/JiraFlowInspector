using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

namespace JiraMetrics.Tests.Presentation;

public sealed class QaTransitionPresentationDataTests
{
    [Fact(DisplayName = "QA presentation data prepares shared summaries and ordered rows")]
    public void CreateWhenAnalysisExistsPreparesSharedData()
    {
        var generatedAt = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(2));
        var slowIssue = CreateTimeline("QA-2", hasPullRequest: true, generatedAt);
        var fastIssue = CreateTimeline("QA-1", hasPullRequest: false, generatedAt);
        var pickupRule = new TransitionMeasurementRule(new StatusName("Ready for QA"), new StatusName("QA"));
        var testingRule = new TransitionMeasurementRule(new StatusName("QA"), new StatusName("Done"));
        var holdRule = new TransitionMeasurementRule(new StatusName("QA Hold"), new StatusName("QA"));
        var analysis = new QaTransitionAnalysis(
            new ItemCount(2),
            [new TransitionMeasurementIssue(fastIssue, pickupRule, generatedAt.AddHours(-4), TimeSpan.FromHours(1))],
            TimeSpan.FromHours(1),
            [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(1))],
            [
                new TransitionMeasurementIssue(fastIssue, testingRule, generatedAt.AddHours(-2), TimeSpan.FromHours(2)),
                new TransitionMeasurementIssue(slowIssue, testingRule, generatedAt.AddHours(-1), TimeSpan.FromHours(4))
            ],
            TimeSpan.FromHours(4),
            [
                new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(1)),
                new IssueTypeDuration75Summary(new IssueTypeName("Bug"), new ItemCount(2), TimeSpan.FromHours(3))
            ],
            [new TransitionMeasurementIssue(slowIssue, holdRule, generatedAt.AddHours(-3), TimeSpan.FromHours(3))],
            TimeSpan.FromHours(3),
            []);
        var reportData = CreateReportData(generatedAt, analysis, slowIssue, fastIssue);

        var result = QaTransitionPresentationData.Create(reportData);

        result.ShouldRender.Should().BeTrue();
        result.DoneCodeIssueCount.Should().Be(1);
        result.RejectedCodeIssueCount.Should().Be(0);
        result.OpenBugCount.Should().Be(3);
        result.OpenBugSummary.Should().Be("3 (P1: 1, P2: 1, P3: 1, P4: 0)");
        result.OpenProdBugSummary.Should().Be("2 (P1: 1, P2: 1, P3: 0, P4: 0)");
        result.DoneBugCount.Should().Be(2);
        result.DoneBugSummary.Should().Be("2 (P1: 0, P2: 1, P3: 0, P4: 1)");
        result.DoneProdBugSummary.Should().Be("1 (P1: 0, P2: 0, P3: 0, P4: 1)");
        result.RejectedBugCount.Should().Be(2);
        result.RejectedBugSummary.Should().Be("2 (P1: 1, P2: 0, P3: 1, P4: 0)");
        result.RejectedProdBugSummary.Should().Be("1 (P1: 1, P2: 0, P3: 0, P4: 0)");
        result.PickupCoverageText.Should().Be("1/2 (50%)");
        result.AqaCoverageText.Should().BeNull();
        result.PickupIssueCountText.Should().Be("1/2");
        result.PickupShareText.Should().Be("50%");
        result.Pickup.RulesLabel.Should().Be("Ready for QA -> QA");
        result.Testing.Issues.Select(static issue => issue.Key.Value)
            .Should().Equal("QA-2", "QA-1");
        result.Testing.Issues[0].IssueUrl.Should().Be("https://example.atlassian.net/browse/QA-2");
        result.Testing.Issues[0].DurationText.Should().Be("4 hours");
        result.Testing.Duration75PerType.Select(static summary => summary.IssueType.Value)
            .Should().Equal("Bug", "Task");
        result.DurationColumnLabel.Should().Be("Hours in QA");
        result.HoldDurationColumnLabel.Should().Be("Hours on hold");
        result.Duration75ColumnLabel.Should().Be("Hours 75P");
        result.Duration75Title.Should().Be("Hours in QA 75P");
    }

    [Fact(DisplayName = "Empty QA presentation data stays hidden and formats missing durations")]
    public void CreateWhenAnalysisIsEmptyReturnsHiddenPresentationData()
    {
        var generatedAt = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(2));
        var settings = CreateSettings(showHoursOnly: false);
        var reportData = new JiraReportData
        {
            RunContext = new ReportRunContext(generatedAt),
            Settings = settings,
            Transitions = new JiraReportTransitionData()
        };

        var result = QaTransitionPresentationData.Create(reportData);

        result.ShouldRender.Should().BeFalse();
        result.OpenBugSummary.Should().Be("0 (P1: 0, P2: 0, P3: 0, P4: 0)");
        result.DoneBugSummary.Should().Be("0 (P1: 0, P2: 0, P3: 0, P4: 0)");
        result.RejectedBugSummary.Should().Be("0 (P1: 0, P2: 0, P3: 0, P4: 0)");
        result.Pickup.Duration75Text.Should().Be("-");
        result.Testing.Duration75Text.Should().Be("-");
        result.Hold.Duration75Text.Should().Be("-");
        result.DurationColumnLabel.Should().Be("Days in QA");
        result.Duration75ColumnLabel.Should().Be("Days 75P");
    }

    private static JiraReportData CreateReportData(
        DateTimeOffset generatedAt,
        QaTransitionAnalysis analysis,
        IssueTimeline doneIssue,
        IssueTimeline rejectedIssue)
    {
        var openP2 = new IssueListItem(
            new IssueKey("BUG-2"),
            new IssueSummary("P2 bug"),
            reporducedOnProd: true,
            priority: "P2");
        var openP1 = new IssueListItem(
            new IssueKey("BUG-1"),
            new IssueSummary("P1 bug"),
            reporducedOnProd: true,
            priority: "P1");
        var openP3 = new IssueListItem(
            new IssueKey("BUG-3"),
            new IssueSummary("P3 bug"),
            priority: "P3");
        var doneP2 = new IssueListItem(
            new IssueKey("BUG-4"),
            new IssueSummary("P2 bug"),
            priority: "P2");
        var doneP4 = new IssueListItem(
            new IssueKey("BUG-5"),
            new IssueSummary("P4 bug"),
            reporducedOnProd: true,
            priority: "P4");
        var rejectedP1 = new IssueListItem(
            new IssueKey("BUG-6"),
            new IssueSummary("P1 bug"),
            reporducedOnProd: true,
            priority: "P1");
        var rejectedP3 = new IssueListItem(
            new IssueKey("BUG-7"),
            new IssueSummary("P3 bug"),
            priority: "P3");

        return new JiraReportData
        {
            RunContext = new ReportRunContext(generatedAt),
            Settings = CreateSettings(showHoursOnly: true),
            Ratios = new JiraReportRatioData
            {
                Bugs = new IssueRatioSnapshot(
                    new ItemCount(7),
                    new ItemCount(3),
                    new ItemCount(2),
                    new ItemCount(2),
                    new ItemCount(4),
                    [openP2, openP1, openP3],
                    [doneP2, doneP4],
                    [rejectedP1, rejectedP3])
            },
            Transitions = new JiraReportTransitionData
            {
                DoneIssues = [doneIssue],
                RejectedIssues = [rejectedIssue],
                QaTransitionAnalysis = analysis
            }
        };
    }

    private static AppSettings CreateSettings(bool showHoursOnly)
    {
        var pickupRule = new TransitionMeasurementRule(new StatusName("Ready for QA"), new StatusName("QA"));
        var testingRule = new TransitionMeasurementRule(new StatusName("QA"), new StatusName("Done"));
        var holdRule = new TransitionMeasurementRule(new StatusName("QA Hold"), new StatusName("QA"));

        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("QA"),
            new StatusName("Done"),
            new StatusName("Rejected"),
            [new StageName("Code Review")],
            new MonthLabel("2026-07"),
            qaTransitionAnalysis: new QaTransitionAnalysisSettings(
                enabled: true,
                [pickupRule],
                [testingRule],
                [holdRule]),
            showTimeCalculationsInHoursOnly: showHoursOnly);
    }

    private static IssueTimeline CreateTimeline(
        string key,
        bool hasPullRequest,
        DateTimeOffset generatedAt) =>
        IssueTimeline.Create(
            new IssueKey(key),
            new IssueTypeName("Task"),
            new IssueSummary($"Issue {key}"),
            generatedAt.AddDays(-1),
            [],
            generatedAt,
            subItemsCount: 2,
            hasPullRequest: hasPullRequest);
}
