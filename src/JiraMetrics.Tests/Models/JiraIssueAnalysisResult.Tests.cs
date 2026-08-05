using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraIssueAnalysisResultTests
{
    [Fact(DisplayName = "Success throws when required arguments are null")]
    [Trait("Category", "Unit")]
    public void SuccessWhenRequiredArgumentIsNullThrowsArgumentNullException()
    {
        var doneIssues = new List<IssueTimeline>();
        var rejectedIssues = new List<IssueTimeline>();
        var summaries = new List<IssueTypeWorkDays75Summary>();
        var customTransitionIssues = new List<CustomTransitionIssue>();
        var customTransitionSummaries = new List<IssueTypeDuration75Summary>();
        var pathGroups = new List<PathGroup>();
        var pathSummary = new PathGroupsSummary(new ItemCount(1), new ItemCount(1), new ItemCount(0), new ItemCount(1));

        Action nullDoneIssues = () => _ = new SuccessfulJiraIssueAnalysis(null!, rejectedIssues, summaries, customTransitionIssues, customTransitionSummaries, pathGroups, pathSummary);
        Action nullRejectedIssues = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, null!, summaries, customTransitionIssues, customTransitionSummaries, pathGroups, pathSummary);
        Action nullSummaries = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, rejectedIssues, null!, customTransitionIssues, customTransitionSummaries, pathGroups, pathSummary);
        Action nullCustomTransitionIssues = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, rejectedIssues, summaries, null!, customTransitionSummaries, pathGroups, pathSummary);
        Action nullCustomTransitionSummaries = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, rejectedIssues, summaries, customTransitionIssues, null!, pathGroups, pathSummary);
        Action nullPathGroups = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, rejectedIssues, summaries, customTransitionIssues, customTransitionSummaries, null!, pathSummary);
        Action nullPathSummary = () => _ = new SuccessfulJiraIssueAnalysis(doneIssues, rejectedIssues, summaries, customTransitionIssues, customTransitionSummaries, pathGroups, null!);

        nullDoneIssues.Should().Throw<ArgumentNullException>();
        nullRejectedIssues.Should().Throw<ArgumentNullException>();
        nullSummaries.Should().Throw<ArgumentNullException>();
        nullCustomTransitionIssues.Should().Throw<ArgumentNullException>();
        nullCustomTransitionSummaries.Should().Throw<ArgumentNullException>();
        nullPathGroups.Should().Throw<ArgumentNullException>();
        nullPathSummary.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Success returns successful analysis result")]
    [Trait("Category", "Unit")]
    public void SuccessWhenArgumentsAreValidReturnsSuccessfulResult()
    {
        var doneIssues = new List<IssueTimeline> { CreateIssueTimeline("AAA-1") };
        var rejectedIssues = new List<IssueTimeline> { CreateIssueTimeline("AAA-2") };
        var summaries =
            new List<IssueTypeWorkDays75Summary> { new(new IssueTypeName("Story"), new ItemCount(1), TimeSpan.FromDays(2)) };
        var customTransitionIssues = new List<CustomTransitionIssue>
        {
            new(doneIssues[0], DateTimeOffset.UtcNow, TimeSpan.FromHours(3))
        };
        var customTransitionSummaries = new List<IssueTypeDuration75Summary>
        {
            new(new IssueTypeName("Story"), new ItemCount(1), TimeSpan.FromHours(3))
        };
        var pathGroups =
            new List<PathGroup> { new(new PathLabel("Open -> Done"), doneIssues, [], TimeSpan.FromDays(2)) };
        var pathSummary = new PathGroupsSummary(new ItemCount(2), new ItemCount(2), new ItemCount(0), new ItemCount(1));

        var result = new SuccessfulJiraIssueAnalysis(
            doneIssues,
            rejectedIssues,
            summaries,
            customTransitionIssues,
            customTransitionSummaries,
            pathGroups,
            pathSummary);

        result.DoneIssues.Should().BeSameAs(doneIssues);
        result.RejectedIssues.Should().BeSameAs(rejectedIssues);
        result.DoneDaysAtWork75PerType.Should().BeSameAs(summaries);
        result.CustomTransitionIssues.Should().BeSameAs(customTransitionIssues);
        result.CustomTransitionDuration75PerType.Should().BeSameAs(customTransitionSummaries);
        result.PathGroups.Should().BeSameAs(pathGroups);
        result.PathSummary.Should().Be(pathSummary);
    }

    [Fact(DisplayName = "NoIssuesMatchedTypeFilter is a typed analysis outcome")]
    [Trait("Category", "Unit")]
    public void NoIssuesMatchedTypeFilterWhenCalledReturnsExpectedOutcome()
    {
        JiraIssueAnalysisResult result = new NoIssuesMatchedTypeFilterAnalysis();

        result.Should().BeOfType<NoIssuesMatchedTypeFilterAnalysis>();
    }

    [Fact(DisplayName = "NoIssuesMatchedRequiredStage is a typed analysis outcome")]
    [Trait("Category", "Unit")]
    public void NoIssuesMatchedRequiredStageWhenCalledReturnsExpectedOutcome()
    {
        JiraIssueAnalysisResult result = new NoIssuesMatchedRequiredStageAnalysis();

        result.Should().BeOfType<NoIssuesMatchedRequiredStageAnalysis>();
    }

    private static IssueTimeline CreateIssueTimeline(string key)
    {
        var created = new DateTimeOffset(2026, 03, 01, 8, 0, 0, TimeSpan.Zero);
        var finished = created.AddHours(2);

        return new IssueTimeline(
            new IssueKey(key),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            finished,
            [],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));
    }
}
