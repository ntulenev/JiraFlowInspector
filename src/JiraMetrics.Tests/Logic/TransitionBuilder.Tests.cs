using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.Logic;

public sealed class TransitionBuilderTests
{
    [Fact(DisplayName = "BuildTransitions returns empty list when there are no raw transitions")]
    [Trait("Category", "Unit")]
    public void BuildTransitionsWhenRawTransitionsAreEmptyReturnsEmpty()
    {
        // Arrange
        var builder = CreateBuilder();
        var created = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);

        // Act
        var transitions = builder.BuildTransitions([], created);

        // Assert
        transitions.Should().BeEmpty();
    }

    [Fact(DisplayName = "BuildTransitions orders transitions and clamps to created time")]
    [Trait("Category", "Unit")]
    public void BuildTransitionsWhenOutOfOrderClampsAndOrders()
    {
        // Arrange
        var builder = CreateBuilder();
        var created = new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
        var rawTransitions = new List<(DateTimeOffset At, StatusName From, StatusName To)>
        {
            (new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero), new StatusName("In Progress"), new StatusName("Done")),
            (new DateTimeOffset(2026, 2, 1, 9, 0, 0, TimeSpan.Zero), new StatusName("Open"), new StatusName("In Progress"))
        };

        // Act
        var transitions = builder.BuildTransitions(rawTransitions, created);

        // Assert
        transitions.Should().HaveCount(2);
        transitions[0].At.Should().Be(created);
        transitions[0].SincePrevious.Should().Be(TimeSpan.Zero);
        transitions[1].At.Should().Be(new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero));
        transitions[1].SincePrevious.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact(DisplayName = "BuildTransitions excludes weekends when configured")]
    [Trait("Category", "Unit")]
    public void BuildTransitionsWhenExcludeWeekendIsTrueSkipsWeekendHours()
    {
        // Arrange
        var builder = CreateBuilder(excludeWeekend: true);
        var created = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.Zero);
        var rawTransitions = new List<(DateTimeOffset At, StatusName From, StatusName To)>
        {
            (new DateTimeOffset(2026, 2, 9, 10, 0, 0, TimeSpan.Zero), new StatusName("Open"), new StatusName("Done"))
        };

        // Act
        var transitions = builder.BuildTransitions(rawTransitions, created);

        // Assert
        transitions.Should().ContainSingle()
            .Which.SincePrevious.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact(DisplayName = "BuildTransitions excludes configured holidays")]
    [Trait("Category", "Unit")]
    public void BuildTransitionsWhenExcludedDaysAreConfiguredSkipsThoseHours()
    {
        // Arrange
        var excludedDays = new List<DateOnly> { new(2026, 2, 3) };
        var builder = CreateBuilder(excludedDays: excludedDays);
        var created = new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
        var rawTransitions = new List<(DateTimeOffset At, StatusName From, StatusName To)>
        {
            (new DateTimeOffset(2026, 2, 4, 10, 0, 0, TimeSpan.Zero), new StatusName("Open"), new StatusName("Done"))
        };

        // Act
        var transitions = builder.BuildTransitions(rawTransitions, created);

        // Assert
        transitions.Should().ContainSingle()
            .Which.SincePrevious.Should().Be(TimeSpan.FromHours(24));
    }

    private static TransitionBuilder CreateBuilder(
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null) => new(CreateSettings(excludeWeekend, excludedDays));

    private static IOptions<AppSettings> CreateSettings(
        bool excludeWeekend = false,
        IReadOnlyList<DateOnly>? excludedDays = null)
    {
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            createdAfter: null,
            issueTypes: null,
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: excludeWeekend,
            excludedDays: excludedDays);

        return Options.Create(settings);
    }
}
