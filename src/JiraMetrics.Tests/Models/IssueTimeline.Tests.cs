using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class IssueTimelineTests
{
    [Fact(DisplayName = "Constructor throws when transitions are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenTransitionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<TransitionEvent> transitions = null!;

        // Act
        Action act = () => _ = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when end time is earlier than created time")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenEndTimeIsEarlierThanCreatedThrowsArgumentException()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow;
        var endTime = created.AddMinutes(-1);

        // Act
        Action act = () => _ = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            endTime,
            [],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when sub-items count is negative")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSubItemsCountIsNegativeThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-1);
        var endTime = DateTimeOffset.UtcNow;

        // Act
        Action act = () => _ = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            endTime,
            [],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"),
            -1);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-2);
        var endTime = DateTimeOffset.UtcNow;
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), endTime, TimeSpan.FromHours(2))
        };

        // Act
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            endTime,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Assert
        issue.Key.Value.Should().Be("AAA-1");
        issue.IssueType.Value.Should().Be("Story");
        issue.Summary.Value.Should().Be("Summary");
        issue.Created.Should().Be(created);
        issue.EndTime.Should().Be(endTime);
        issue.Transitions.Should().BeSameAs(transitions);
        issue.PathKey.Value.Should().Be("OPEN->DONE");
        issue.PathLabel.Value.Should().Be("Open -> Done");
        issue.SubItemsCount.Should().Be(0);
        issue.HasPullRequest.Should().BeFalse();
    }

    [Fact(DisplayName = "TryBuildWorkDuration returns cumulative duration until target status")]
    [Trait("Category", "Unit")]
    public void TryBuildWorkDurationWhenTargetStatusIsReachedReturnsDuration()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-3);
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            DateTimeOffset.UtcNow,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("In Progress"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1)),
                new TransitionEvent(
                    new StatusName("In Progress"),
                    new StatusName("Done"),
                    created.AddHours(3),
                    TimeSpan.FromHours(2))
            ],
            new PathKey("OPEN->IN PROGRESS->DONE"),
            new PathLabel("Open -> In Progress -> Done"));

        // Act
        var duration = issue.TryBuildWorkDuration(new StatusName("Done"));

        // Assert
        duration.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact(DisplayName = "TryBuildWorkDuration returns null when target status was not reached")]
    [Trait("Category", "Unit")]
    public void TryBuildWorkDurationWhenTargetStatusWasNotReachedReturnsNull()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-2);
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            DateTimeOffset.UtcNow,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("In Progress"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1))
            ],
            new PathKey("OPEN->IN PROGRESS"),
            new PathLabel("Open -> In Progress"));

        // Act
        var duration = issue.TryBuildWorkDuration(new StatusName("Done"));

        // Assert
        duration.Should().BeNull();
    }

    [Fact(DisplayName = "TryBuildWorkDuration clamps negative total duration to zero")]
    [Trait("Category", "Unit")]
    public void TryBuildWorkDurationWhenTotalDurationIsNegativeReturnsZero()
    {
        // Arrange
        var created = DateTimeOffset.UtcNow.AddHours(-1);
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            DateTimeOffset.UtcNow,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("Done"),
                    created.AddMinutes(10),
                    TimeSpan.FromHours(-1))
            ],
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Act
        var duration = issue.TryBuildWorkDuration(new StatusName("Done"));

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact(DisplayName = "Create derives path metadata and clamps end time")]
    [Trait("Category", "Unit")]
    public void CreateWhenEndTimeIsEarlierThanCreatedNormalizesTimeline()
    {
        // Arrange
        var created = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var transitions = new List<TransitionEvent>
        {
            new(
                new StatusName("Open"),
                new StatusName("Code Review"),
                created.AddHours(2),
                TimeSpan.FromHours(2))
        };

        // Act
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            transitions,
            endTime: created.AddMinutes(-5),
            subItemsCount: 2,
            hasPullRequest: true);

        // Assert
        issue.EndTime.Should().Be(created);
        issue.PathKey.Should().Be(PathKey.FromTransitions(transitions));
        issue.PathLabel.Should().Be(PathLabel.FromTransitions(transitions));
        issue.SubItemsCount.Should().Be(2);
        issue.HasPullRequest.Should().BeTrue();
    }

    [Fact(DisplayName = "TryGetLastReachedAt returns timestamp of the latest matching status transition")]
    [Trait("Category", "Unit")]
    public void TryGetLastReachedAtWhenStatusWasReachedReturnsLatestTimestamp()
    {
        // Arrange
        var created = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("In Progress"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1)),
                new TransitionEvent(
                    new StatusName("In Progress"),
                    new StatusName("Done"),
                    created.AddHours(2),
                    TimeSpan.FromHours(1)),
                new TransitionEvent(
                    new StatusName("Done"),
                    new StatusName("Done"),
                    created.AddHours(3),
                    TimeSpan.FromHours(1))
            ],
            endTime: created.AddHours(4));

        // Act
        var lastReachedAt = issue.TryGetLastReachedAt(new StatusName("Done"));

        // Assert
        lastReachedAt.Should().Be(created.AddHours(3));
    }

    [Fact(DisplayName = "MatchesAllStages returns true only when every required stage is used")]
    [Trait("Category", "Unit")]
    public void MatchesAllStagesWhenAnyRequiredStageIsMissingReturnsFalse()
    {
        // Arrange
        var created = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("Code Review"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1)),
                new TransitionEvent(
                    new StatusName("Code Review"),
                    new StatusName("Done"),
                    created.AddHours(2),
                    TimeSpan.FromHours(1))
            ],
            endTime: created.AddHours(3));

        // Act
        var matchesAllStages = issue.MatchesAllStages(
            [new StageName("Code Review"), new StageName("Deploy")]);

        // Assert
        matchesAllStages.Should().BeFalse();
    }

    [Fact(DisplayName = "UsesStage returns true when stage is present in transitions")]
    [Trait("Category", "Unit")]
    public void UsesStageWhenStageIsPresentReturnsTrue()
    {
        // Arrange
        var created = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("Code Review"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1))
            ],
            endTime: created.AddHours(2));

        // Act
        var usesStage = issue.UsesStage(new StageName("Code Review"));

        // Assert
        usesStage.Should().BeTrue();
    }

    [Fact(DisplayName = "IsOfType returns true when issue type matches ignoring case")]
    [Trait("Category", "Unit")]
    public void IsOfTypeWhenIssueTypeMatchesIgnoringCaseReturnsTrue()
    {
        // Arrange
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Bug"),
            new IssueSummary("Summary"),
            DateTimeOffset.UtcNow.AddHours(-2),
            [],
            endTime: DateTimeOffset.UtcNow);

        // Act
        var isOfType = issue.IsOfType(new IssueTypeName("bug"));

        // Assert
        isOfType.Should().BeTrue();
    }

    [Fact(DisplayName = "MatchesAnyType returns true when one of provided types matches")]
    [Trait("Category", "Unit")]
    public void MatchesAnyTypeWhenAnyTypeMatchesReturnsTrue()
    {
        // Arrange
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Bug"),
            new IssueSummary("Summary"),
            DateTimeOffset.UtcNow.AddHours(-2),
            [],
            endTime: DateTimeOffset.UtcNow);

        // Act
        var matchesAnyType = issue.MatchesAnyType(
            [new IssueTypeName("Story"), new IssueTypeName("Bug")]);

        // Assert
        matchesAnyType.Should().BeTrue();
    }

    [Fact(DisplayName = "TryGetLastReachedAt returns null when status was not reached")]
    [Trait("Category", "Unit")]
    public void TryGetLastReachedAtWhenStatusWasNotReachedReturnsNull()
    {
        // Arrange
        var created = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var issue = IssueTimeline.Create(
            new IssueKey("AAA-1"),
            new IssueTypeName("Story"),
            new IssueSummary("Summary"),
            created,
            [
                new TransitionEvent(
                    new StatusName("Open"),
                    new StatusName("In Progress"),
                    created.AddHours(1),
                    TimeSpan.FromHours(1))
            ],
            endTime: created.AddHours(2));

        // Act
        var lastReachedAt = issue.TryGetLastReachedAt(new StatusName("Done"));

        // Assert
        lastReachedAt.Should().BeNull();
    }
}
