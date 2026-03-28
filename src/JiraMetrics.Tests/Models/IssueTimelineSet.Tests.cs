using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTimelineSetTests
{
    [Fact(DisplayName = "FilterByRequiredStages returns only issues matching all stages")]
    [Trait("Category", "Unit")]
    public void FilterByRequiredStagesWhenCalledReturnsOnlyMatchingIssues()
    {
        // Arrange
        var matchingIssue = CreateIssue(
            "AAA-1",
            "Story",
            hasPullRequest: false,
            [
                ("Open", "Code Review", 1),
                ("Code Review", "Done", 2)
            ]);
        var nonMatchingIssue = CreateIssue(
            "AAA-2",
            "Story",
            hasPullRequest: false,
            [
                ("Open", "In Progress", 1),
                ("In Progress", "Done", 2)
            ]);
        var set = new IssueTimelineSet([matchingIssue, nonMatchingIssue]);

        // Act
        var filtered = set.FilterByRequiredStages(
            [new StageName("Code Review"), new StageName("Done")]);

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Key.Value.Should().Be("AAA-1");
    }

    [Fact(DisplayName = "FilterByIssueTypes returns only issues with matching types")]
    [Trait("Category", "Unit")]
    public void FilterByIssueTypesWhenCalledReturnsOnlyMatchingIssueTypes()
    {
        // Arrange
        var bugIssue = CreateIssue("AAA-1", "Bug", false, [("Open", "Done", 1)]);
        var taskIssue = CreateIssue("AAA-2", "Task", false, [("Open", "Done", 1)]);
        var set = new IssueTimelineSet([bugIssue, taskIssue]);

        // Act
        var filtered = set.FilterByIssueTypes([new IssueTypeName("Bug")]);

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Key.Value.Should().Be("AAA-1");
    }

    [Fact(DisplayName = "WithPullRequests returns only issues with pull request activity")]
    [Trait("Category", "Unit")]
    public void WithPullRequestsWhenCalledReturnsOnlyIssuesWithPullRequests()
    {
        // Arrange
        var withCode = CreateIssue("AAA-1", "Task", true, [("Open", "Done", 1)]);
        var withoutCode = CreateIssue("AAA-2", "Task", false, [("Open", "Done", 1)]);
        var set = new IssueTimelineSet([withCode, withoutCode]);

        // Act
        var filtered = set.WithPullRequests();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Key.Value.Should().Be("AAA-1");
    }

    [Fact(DisplayName = "BuildDaysAtWork75PerType calculates p75 per issue type")]
    [Trait("Category", "Unit")]
    public void BuildDaysAtWork75PerTypeWhenCalledBuildsSummaries()
    {
        // Arrange
        var taskIssueOne = CreateIssue(
            "AAA-1",
            "Task",
            false,
            [
                ("Open", "In Progress", 1),
                ("In Progress", "Done", 25)
            ]);
        var taskIssueTwo = CreateIssue(
            "AAA-2",
            "Task",
            false,
            [
                ("Open", "In Progress", 2),
                ("In Progress", "Done", 50)
            ]);
        var bugIssue = CreateIssue("AAA-3", "Bug", false, [("Open", "Done", 12)]);
        var set = new IssueTimelineSet([taskIssueOne, taskIssueTwo, bugIssue]);

        // Act
        var summaries = set.BuildDaysAtWork75PerType(
            new StatusName("Done"),
            static (samples, percentile) => new JiraMetrics.Logic.JiraAnalyticsService()
                .CalculatePercentile(samples, percentile));

        // Assert
        summaries.Should().HaveCount(2);
        summaries[0].IssueType.Value.Should().Be("Task");
        summaries[0].IssueCount.Value.Should().Be(2);
        summaries[0].DaysAtWorkP75.TotalHours.Should().Be(43.75);
        summaries[1].IssueType.Value.Should().Be("Bug");
    }

    [Fact(DisplayName = "BuildPathGroups groups issues by path and calculates total p75")]
    [Trait("Category", "Unit")]
    public void BuildPathGroupsWhenCalledBuildsGroupedAnalytics()
    {
        // Arrange
        var issueOne = CreateIssue(
            "AAA-1",
            "Task",
            true,
            [
                ("Open", "Code Review", 2),
                ("Code Review", "Done", 6)
            ]);
        var issueTwo = CreateIssue(
            "AAA-2",
            "Task",
            true,
            [
                ("Open", "Code Review", 6),
                ("Code Review", "Done", 10)
            ]);
        var set = new IssueTimelineSet([issueOne, issueTwo]);

        // Act
        var groups = set.BuildPathGroups(
            static (samples, percentile) => new JiraMetrics.Logic.JiraAnalyticsService()
                .CalculatePercentile(samples, percentile));

        // Assert
        groups.Should().ContainSingle();
        groups[0].Issues.Should().HaveCount(2);
        groups[0].P75Transitions.Should().HaveCount(2);
        groups[0].P75Transitions[0].P75Duration.Should().Be(TimeSpan.FromHours(5));
        groups[0].P75Transitions[1].P75Duration.Should().Be(TimeSpan.FromHours(4));
        groups[0].TotalP75.Should().Be(TimeSpan.FromHours(9));
    }

    private static IssueTimeline CreateIssue(
        string key,
        string issueType,
        bool hasPullRequest,
        IReadOnlyList<(string from, string to, int hoursFromCreated)> transitions)
    {
        var created = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var transitionEvents = transitions
            .Select((transition, index) => new TransitionEvent(
                new StatusName(transition.from),
                new StatusName(transition.to),
                created.AddHours(transition.hoursFromCreated),
                index == 0
                    ? TimeSpan.FromHours(transition.hoursFromCreated)
                    : TimeSpan.FromHours(
                        transition.hoursFromCreated - transitions[index - 1].hoursFromCreated)))
            .ToArray();

        return IssueTimeline.Create(
            new IssueKey(key),
            new IssueTypeName(issueType),
            new IssueSummary($"Summary {key}"),
            created,
            transitionEvents,
            endTime: created.AddHours(transitionEvents.Length == 0
                ? 1
                : transitions[^1].hoursFromCreated + 1),
            hasPullRequest: hasPullRequest);
    }
}
