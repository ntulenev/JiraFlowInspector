using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

using Spectre.Console;
using Spectre.Console.Testing;

namespace JiraMetrics.Tests.Presentation;

public sealed class SpectreJiraPresentationServiceTests
{
    [Fact(DisplayName = "Constructor throws when analytics service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAnalyticsServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        IJiraAnalyticsService analyticsService = null!;

        // Act
        Action act = () => _ = new SpectreJiraPresentationService(analyticsService);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowAuthenticationSucceeded writes user display name")]
    [Trait("Category", "Unit")]
    public async Task ShowAuthenticationSucceededWhenCalledWritesDisplayName()
    {
        // Arrange
        var service = new SpectreJiraPresentationService(new JiraAnalyticsService());

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
        var service = new SpectreJiraPresentationService(new JiraAnalyticsService());
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
        var service = new SpectreJiraPresentationService(new JiraAnalyticsService());

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
        var service = new SpectreJiraPresentationService(new JiraAnalyticsService());
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

