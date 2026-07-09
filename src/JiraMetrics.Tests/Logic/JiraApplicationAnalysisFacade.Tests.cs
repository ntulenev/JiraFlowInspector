using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationAnalysisFacadeTests
{
    [Fact(DisplayName = "Analyze builds QA transition analysis from filtered done and rejected issues")]
    [Trait("Category", "Unit")]
    public void AnalyzeWhenQaTransitionsExistBuildsQaTransitionAnalysis()
    {
        // Arrange
        var facade = new JiraApplicationAnalysisFacade(new JiraLogicService(new JiraAnalyticsService()));
        var now = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var pickedUpAndTested = CreateIssue(
            "AAA-1",
            [
                new TransitionEvent(new StatusName("Ready for QA"), new StatusName("Testing"), now.AddHours(-8), TimeSpan.FromHours(2)),
                new TransitionEvent(new StatusName("Testing"), new StatusName("Ready for release"), now.AddHours(-4), TimeSpan.FromHours(6)),
                new TransitionEvent(new StatusName("Ready for release"), new StatusName("Done"), now, TimeSpan.FromHours(1))
            ]);
        var skippedQaInProgress = CreateIssue(
            "AAA-2",
            [
                new TransitionEvent(new StatusName("Manual QA"), new StatusName("Ready for release"), now.AddHours(-4), TimeSpan.FromHours(4)),
                new TransitionEvent(new StatusName("Ready for release"), new StatusName("Done"), now, TimeSpan.FromHours(1))
            ]);
        var skippedQaInProgressWithoutExactEdge = CreateIssue(
            "AAA-5",
            [
                new TransitionEvent(new StatusName("Code Review"), new StatusName("Manual QA"), now.AddHours(-10), TimeSpan.FromHours(1)),
                new TransitionEvent(new StatusName("Manual QA"), new StatusName("Waiting for release"), now.AddHours(-6), TimeSpan.FromHours(4)),
                new TransitionEvent(new StatusName("Waiting for release"), new StatusName("Ready for release"), now.AddHours(-1), TimeSpan.FromHours(5)),
                new TransitionEvent(new StatusName("Ready for release"), new StatusName("Done"), now, TimeSpan.FromHours(1))
            ]);
        var noCodeQaIssue = CreateIssue(
            "AAA-4",
            [
                new TransitionEvent(new StatusName("Ready for QA"), new StatusName("Testing"), now.AddHours(-20), TimeSpan.FromHours(20)),
                new TransitionEvent(new StatusName("Testing"), new StatusName("Ready for release"), now.AddHours(-1), TimeSpan.FromHours(20)),
                new TransitionEvent(new StatusName("Ready for release"), new StatusName("Done"), now, TimeSpan.FromHours(1))
            ],
            hasPullRequest: false);
        var rejectedWithPickup = CreateIssue(
            "AAA-3",
            [
                new TransitionEvent(new StatusName("Ready for QA"), new StatusName("Testing"), now.AddHours(-10), TimeSpan.FromHours(10)),
                new TransitionEvent(new StatusName("Testing"), new StatusName("Ready for release"), now.AddHours(-2), TimeSpan.FromHours(8)),
                new TransitionEvent(new StatusName("Ready for release"), new StatusName("Rejected"), now, TimeSpan.FromHours(1))
            ]);
        var settings = CreateSettings();

        // Act
        var result = facade.Analyze(
            [pickedUpAndTested, skippedQaInProgress, skippedQaInProgressWithoutExactEdge, noCodeQaIssue],
            [rejectedWithPickup],
            [],
            settings);

        // Assert
        result.Outcome.Should().Be(JiraIssueAnalysisOutcome.Success);
        result.QaTransitionAnalysis.AnalyzedIssueCount.Should().Be(new ItemCount(4));
        result.QaTransitionAnalysis.PickupIssues.Select(static item => item.Issue.Key.Value).Should().Equal("AAA-3", "AAA-1");
        result.QaTransitionAnalysis.PickupIssuePercentage.Should().Be(50m);
        result.QaTransitionAnalysis.PickupDuration75.Should().Be(TimeSpan.FromHours(8));
        result.QaTransitionAnalysis.TestingIssues.Select(static item => item.Issue.Key.Value).Should().Equal("AAA-5", "AAA-3", "AAA-1", "AAA-2");
        result.QaTransitionAnalysis.TestingIssues[0].Rule.Label.Should().Be("Manual QA -> Ready for release");
        result.QaTransitionAnalysis.TestingIssues[0].Duration.Should().Be(TimeSpan.FromHours(9));
        result.QaTransitionAnalysis.TestingIssues[3].Rule.Label.Should().Be("Manual QA -> Ready for release");
        result.QaTransitionAnalysis.TestingDuration75.Should().Be(TimeSpan.FromHours(8.25));
        result.QaTransitionAnalysis.HoldIssues.Select(static item => item.Issue.Key.Value).Should().Equal("AAA-5");
        result.QaTransitionAnalysis.HoldDuration75.Should().Be(TimeSpan.FromHours(5));
    }

    private static AppSettings CreateSettings() =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Rejected"),
            [new StageName("Ready for release")],
            new MonthLabel("2026-03"),
            qaTransitionAnalysis: new QaTransitionAnalysisSettings(
                enabled: true,
                pickupTransitions:
                [
                    new TransitionMeasurementRule(
                        new StatusName("Ready for QA"),
                        new StatusName("Testing"))
                ],
                testingTransitions:
                [
                    new TransitionMeasurementRule(
                        new StatusName("Testing"),
                        new StatusName("Ready for release")),
                    new TransitionMeasurementRule(
                        new StatusName("Manual QA"),
                        new StatusName("Ready for release"))
                ],
                holdTransitions:
                [
                    new TransitionMeasurementRule(
                        new StatusName("Waiting for release"),
                        new StatusName("Ready for release"))
                ]));

    private static IssueTimeline CreateIssue(
        string key,
        IReadOnlyList<TransitionEvent> transitions,
        bool hasPullRequest = true)
    {
        return new IssueTimeline(
            new IssueKey(key),
            new IssueTypeName("Story"),
            new IssueSummary($"Summary {key}"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: hasPullRequest);
    }
}
