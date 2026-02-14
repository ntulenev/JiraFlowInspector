using FluentAssertions;

using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Configuration;

public sealed class AppSettingsTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var baseUrl = new JiraBaseUrl("https://example.atlassian.net");
        var email = new JiraEmail("user@example.com");
        var token = new JiraApiToken("token");
        var projectKey = new ProjectKey("AAA");
        var doneStatus = new StatusName("Done");
        var requiredPathStage = new StageName("Code Review");
        var monthLabel = new MonthLabel("2026-02");
        var createdAfter = new CreatedAfterDate("2026-01-15");
        var issueTypes = new List<IssueTypeName> { new("Bug"), new("Story") };
        var excludedDays = new List<DateOnly> { new(2026, 2, 3), new(2026, 2, 4) };

        // Act
        var settings = new AppSettings(
            baseUrl,
            email,
            token,
            projectKey,
            doneStatus,
            requiredPathStage,
            monthLabel,
            createdAfter,
            issueTypes,
            excludeWeekend: true,
            excludedDays: excludedDays);

        // Assert
        settings.BaseUrl.Should().Be(baseUrl);
        settings.Email.Should().Be(email);
        settings.ApiToken.Should().Be(token);
        settings.ProjectKey.Should().Be(projectKey);
        settings.DoneStatusName.Should().Be(doneStatus);
        settings.RequiredPathStage.Should().Be(requiredPathStage);
        settings.MonthLabel.Should().Be(monthLabel);
        settings.CreatedAfter.Should().Be(createdAfter);
        settings.IssueTypes.Select(static issueType => issueType.Value).Should().ContainInOrder("Bug", "Story");
        settings.ExcludeWeekend.Should().BeTrue();
        settings.ExcludedDays.Should().ContainInOrder(excludedDays);
    }
}
