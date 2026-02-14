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
        var requiredPathStages = new List<StageName> { new("Code Review"), new("QA") };
        var monthLabel = new MonthLabel("2026-02");
        var createdAfter = new CreatedAfterDate("2026-01-15");
        var issueTypes = new List<IssueTypeName> { new("Bug"), new("Story") };
        var excludedDays = new List<DateOnly> { new(2026, 2, 3), new(2026, 2, 4) };
        const string customFieldName = "Team";
        const string customFieldValue = "Import";

        // Act
        var settings = new AppSettings(
            baseUrl,
            email,
            token,
            projectKey,
            doneStatus,
            requiredPathStages,
            monthLabel,
            createdAfter,
            issueTypes,
            customFieldName,
            customFieldValue,
            excludeWeekend: true,
            excludedDays: excludedDays);

        // Assert
        settings.BaseUrl.Should().Be(baseUrl);
        settings.Email.Should().Be(email);
        settings.ApiToken.Should().Be(token);
        settings.ProjectKey.Should().Be(projectKey);
        settings.DoneStatusName.Should().Be(doneStatus);
        settings.RequiredPathStages.Should().ContainInOrder(requiredPathStages);
        settings.MonthLabel.Should().Be(monthLabel);
        settings.CreatedAfter.Should().Be(createdAfter);
        settings.IssueTypes.Select(static issueType => issueType.Value).Should().ContainInOrder("Bug", "Story");
        settings.CustomFieldName.Should().Be(customFieldName);
        settings.CustomFieldValue.Should().Be(customFieldValue);
        settings.ExcludeWeekend.Should().BeTrue();
        settings.ExcludedDays.Should().ContainInOrder(excludedDays);
    }
}
