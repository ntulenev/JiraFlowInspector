using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

using Spectre.Console;
using Spectre.Console.Testing;

namespace JiraMetrics.Tests.Presentation;

public sealed class SpectreJiraPresentationServiceTests
{
    [Fact(DisplayName = "ShowAuthenticationSucceeded throws when user is null")]
    [Trait("Category", "Unit")]
    public void ShowAuthenticationSucceededWhenUserIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        JiraAuthUser user = null!;

        // Act
        Action act = () => service.ShowAuthenticationSucceeded(user);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowReportHeader throws when settings are null")]
    [Trait("Category", "Unit")]
    public void ShowReportHeaderWhenSettingsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        AppSettings settings = null!;

        // Act
        Action act = () => service.ShowReportHeader(settings, new ItemCount(0));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowDoneIssuesTable throws when issues are null")]
    [Trait("Category", "Unit")]
    public void ShowDoneIssuesTableWhenIssuesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<IssueTimeline> issues = null!;

        // Act
        Action act = () => service.ShowDoneIssuesTable(issues, new StatusName("Done"));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowPathGroupsSummary throws when summary is null")]
    [Trait("Category", "Unit")]
    public void ShowPathGroupsSummaryWhenSummaryIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        PathGroupsSummary summary = null!;

        // Act
        Action act = () => service.ShowPathGroupsSummary(summary);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowPathGroups throws when groups are null")]
    [Trait("Category", "Unit")]
    public void ShowPathGroupsWhenGroupsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<PathGroup> groups = null!;

        // Act
        Action act = () => service.ShowPathGroups(groups);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowFailures throws when failures are null")]
    [Trait("Category", "Unit")]
    public void ShowFailuresWhenFailuresAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<LoadFailure> failures = null!;

        // Act
        Action act = () => service.ShowFailures(failures);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowAuthenticationSucceeded writes user display name")]
    [Trait("Category", "Unit")]
    public async Task ShowAuthenticationSucceededWhenCalledWritesDisplayName()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowAuthenticationSucceeded(new JiraAuthUser(new UserDisplayName("Jane Doe"), null, null));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Auth succeeded for user:");
        output.Should().Contain("Jane Doe");
    }

    [Fact(DisplayName = "ShowFailures writes failed issues table when failures exist")]
    [Trait("Category", "Unit")]
    public async Task ShowFailuresWhenFailuresExistWritesTable()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var failures = new List<LoadFailure>
        {
            new(new IssueKey("AAA-1"), new ErrorMessage("boom"))
        };

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowFailures(failures);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Failed issues");
        output.Should().Contain("AAA-1");
        output.Should().Contain("boom");
    }

    [Fact(DisplayName = "ShowFailures writes nothing when list is empty")]
    [Trait("Category", "Unit")]
    public async Task ShowFailuresWhenListIsEmptyWritesNothing()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowFailures([]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().BeEmpty();
    }

    [Fact(DisplayName = "ShowDoneIssuesTable writes issue type column and value")]
    [Trait("Category", "Unit")]
    public async Task ShowDoneIssuesTableWhenCalledWritesIssueType()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Bug"),
            new IssueSummary("Fix login"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"));

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowDoneIssuesTable([issue], new StatusName("Done"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Type");
        output.Should().Contain("Bug");
    }

    [Fact(DisplayName = "ShowReportHeader writes issue type filter when configured")]
    [Trait("Category", "Unit")]
    public async Task ShowReportHeaderWhenIssueTypesAreConfiguredWritesIssueTypesLine()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            null,
            [new IssueTypeName("Bug"), new IssueTypeName("Story")],
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: false);

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReportHeader(settings, new ItemCount(2));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Issue types:");
        output.Should().Contain("Bug, Story");
    }

    [Fact(DisplayName = "ShowBugRatio writes bug ratio section")]
    [Trait("Category", "Unit")]
    public async Task ShowBugRatioWhenCalledWritesBugRatioSection()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowBugRatio(
                [new IssueTypeName("Bug")],
                new ItemCount(8),
                new ItemCount(4),
                new ItemCount(1),
                new ItemCount(5),
                [new IssueListItem(new IssueKey("AAA-1"), new IssueSummary("Open issue"))],
                [new IssueListItem(new IssueKey("AAA-2"), new IssueSummary("Done issue"))],
                [new IssueListItem(new IssueKey("AAA-3"), new IssueSummary("Rejected issue"))]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Bug ratio");
        output.Should().NotContain("Created this month");
        output.Should().Contain("Open this month");
        output.Should().Contain("Done this month");
        output.Should().Contain("Rejected this month");
        output.Should().Contain("Finished this month");
        output.Should().Contain("62.5%");
        output.Should().Contain("Open issues");
        output.Should().Contain("Done issues");
        output.Should().Contain("Rejected issues");
        output.Should().Contain("Jira ID");
        output.Should().Contain("Title");
        output.Should().Contain("AAA-1");
        output.Should().Contain("AAA-2");
        output.Should().Contain("AAA-3");
    }

    [Fact(DisplayName = "ShowBugRatioLoadingStarted and completed write loader lines")]
    [Trait("Category", "Unit")]
    public async Task ShowBugRatioLoadingMessagesWhenCalledWriteOutput()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowBugRatioLoadingStarted([new IssueTypeName("Bug")]);
            service.ShowBugRatioLoadingCompleted(new ItemCount(3), new ItemCount(2), new ItemCount(1), new ItemCount(3));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading bug ratio data");
        output.Should().Contain("Bug");
        output.Should().Contain("Bug ratio data loaded");
        output.Should().Contain("created = 3");
        output.Should().Contain("done = 2");
        output.Should().Contain("rejected = 1");
        output.Should().Contain("finished = 3");
    }

    [Fact(DisplayName = "Issue loading progress does not print Jira IDs")]
    [Trait("Category", "Unit")]
    public async Task ShowIssueLoadingProgressWhenCalledDoesNotPrintIssueIds()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowIssueLoadingStarted(new ItemCount(3));
            service.ShowIssueLoaded(new IssueKey("AAA-1"));
            service.ShowIssueFailed(new IssueKey("AAA-2"));
            service.ShowIssueLoaded(new IssueKey("AAA-3"));
            service.ShowIssueLoadingCompleted(new ItemCount(2), new ItemCount(1));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading issue timelines");
        output.Should().Contain("Issue loading completed");
        output.Should().NotContain("AAA-1");
        output.Should().NotContain("AAA-2");
        output.Should().NotContain("AAA-3");
    }

    private static async Task<T> RunWithTestConsoleAsync<T>(Func<TestConsole, Task<T>> action)
    {
        var original = AnsiConsole.Console;
        var console = new TestConsole();
        AnsiConsole.Console = console;

        try
        {
            return await action(console);
        }
        finally
        {
            AnsiConsole.Console = original;
        }
    }
}

