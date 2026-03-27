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
        var pathGroups = new List<PathGroup>();
        var pathSummary = new PathGroupsSummary(new ItemCount(1), new ItemCount(1), new ItemCount(0), new ItemCount(1));

        Action nullDoneIssues = () => _ = JiraIssueAnalysisResult.Success(null!, rejectedIssues, summaries, pathGroups, pathSummary);
        Action nullRejectedIssues = () => _ = JiraIssueAnalysisResult.Success(doneIssues, null!, summaries, pathGroups, pathSummary);
        Action nullSummaries = () => _ = JiraIssueAnalysisResult.Success(doneIssues, rejectedIssues, null!, pathGroups, pathSummary);
        Action nullPathGroups = () => _ = JiraIssueAnalysisResult.Success(doneIssues, rejectedIssues, summaries, null!, pathSummary);
        Action nullPathSummary = () => _ = JiraIssueAnalysisResult.Success(doneIssues, rejectedIssues, summaries, pathGroups, null!);

        nullDoneIssues.Should().Throw<ArgumentNullException>();
        nullRejectedIssues.Should().Throw<ArgumentNullException>();
        nullSummaries.Should().Throw<ArgumentNullException>();
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
        var pathGroups =
            new List<PathGroup> { new(new PathLabel("Open -> Done"), doneIssues, [], TimeSpan.FromDays(2)) };
        var pathSummary = new PathGroupsSummary(new ItemCount(2), new ItemCount(2), new ItemCount(0), new ItemCount(1));

        var result = JiraIssueAnalysisResult.Success(doneIssues, rejectedIssues, summaries, pathGroups, pathSummary);

        result.Outcome.Should().Be(JiraIssueAnalysisOutcome.Success);
        result.DoneIssues.Should().BeSameAs(doneIssues);
        result.RejectedIssues.Should().BeSameAs(rejectedIssues);
        result.DoneDaysAtWork75PerType.Should().BeSameAs(summaries);
        result.PathGroups.Should().BeSameAs(pathGroups);
        result.PathSummary.Should().Be(pathSummary);
    }

    [Fact(DisplayName = "NoIssuesMatchedTypeFilter returns matching outcome")]
    [Trait("Category", "Unit")]
    public void NoIssuesMatchedTypeFilterWhenCalledReturnsExpectedOutcome()
    {
        var result = JiraIssueAnalysisResult.NoIssuesMatchedTypeFilter();

        result.Outcome.Should().Be(JiraIssueAnalysisOutcome.NoIssuesMatchedTypeFilter);
        result.DoneIssues.Should().BeEmpty();
        result.RejectedIssues.Should().BeEmpty();
        result.DoneDaysAtWork75PerType.Should().BeEmpty();
        result.PathGroups.Should().BeEmpty();
        result.PathSummary.Should().BeNull();
    }

    [Fact(DisplayName = "NoIssuesMatchedRequiredStage returns matching outcome")]
    [Trait("Category", "Unit")]
    public void NoIssuesMatchedRequiredStageWhenCalledReturnsExpectedOutcome()
    {
        var result = JiraIssueAnalysisResult.NoIssuesMatchedRequiredStage();

        result.Outcome.Should().Be(JiraIssueAnalysisOutcome.NoIssuesMatchedRequiredStage);
        result.DoneIssues.Should().BeEmpty();
        result.RejectedIssues.Should().BeEmpty();
        result.DoneDaysAtWork75PerType.Should().BeEmpty();
        result.PathGroups.Should().BeEmpty();
        result.PathSummary.Should().BeNull();
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
