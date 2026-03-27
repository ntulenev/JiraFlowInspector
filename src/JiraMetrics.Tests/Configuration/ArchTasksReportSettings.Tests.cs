using FluentAssertions;

using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Configuration;

public sealed class ArchTasksReportSettingsTests
{
    [Fact(DisplayName = "Constructor trims JQL")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenJqlContainsPaddingStoresTrimmedValue()
    {
        // Act
        var settings = new ArchTasksReportSettings("  project = AAA ORDER BY created ASC  ");

        // Assert
        settings.Jql.Should().Be("project = AAA ORDER BY created ASC");
    }

    [Fact(DisplayName = "BuildJql replaces monthly resolved clause token")]
    [Trait("Category", "Unit")]
    public void BuildJqlWhenTemplateContainsMonthResolvedClauseReplacesIt()
    {
        // Arrange
        var settings = new ArchTasksReportSettings(
            "project = AAA AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC");

        // Act
        var jql = settings.BuildJql(new MonthLabel("2026-03"));

        // Assert
        jql.Should().Be(
            "project = AAA AND (resolved IS EMPTY OR (resolved >= \"2026-03-01\" AND resolved < \"2026-04-01\")) ORDER BY created ASC");
    }
}
