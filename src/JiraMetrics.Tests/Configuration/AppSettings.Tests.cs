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
        var rejectStatus = new StatusName("Reject");
        var requiredPathStages = new List<StageName> { new("Code Review"), new("QA") };
        var monthLabel = new MonthLabel("2026-02");
        var createdAfter = new CreatedAfterDate("2026-01-15");
        var issueTypes = new List<IssueTypeName> { new("Bug"), new("Story") };
        var bugIssueNames = new List<IssueTypeName> { new("Bug") };
        var releaseReport = new ReleaseReportSettings(new ProjectKey("RLS"), "Processing", "Change completion date");
        var archTasksReport = new ArchTasksReportSettings(
            "project = AAA AND type = \"Arch Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC");
        var globalIncidentsReport = new GlobalIncidentsReportSettings(
            namespaceName: "Incidents",
            jqlFilter: "labels = SERVICE",
            searchPhrase: "ORX disab",
            additionalFieldNames: ["Business Impact"]);
        var pdfReport = new PdfReportSettings(enabled: true, outputPath: "report.pdf");
        var excludedDays = new List<DateOnly> { new(2026, 2, 3), new(2026, 2, 4) };
        const string customFieldName = "Team";
        const string customFieldValue = "Import";
        const string pullRequestFieldName = "customfield_22222";

        // Act
        var settings = new AppSettings(
            baseUrl,
            email,
            token,
            projectKey,
            doneStatus,
            rejectStatus,
            requiredPathStages,
            monthLabel,
            createdAfter,
            issueTypes,
            customFieldName,
            customFieldValue,
            showTimeCalculationsInHoursOnly: true,
            excludeWeekend: true,
            excludedDays: excludedDays,
            bugIssueNames: bugIssueNames,
            showGeneralStatistics: false,
            releaseReport: releaseReport,
            archTasksReport: archTasksReport,
            globalIncidentsReport: globalIncidentsReport,
            pdfReport: pdfReport,
            pullRequestFieldName: pullRequestFieldName);

        // Assert
        settings.BaseUrl.Should().Be(baseUrl);
        settings.Email.Should().Be(email);
        settings.ApiToken.Should().Be(token);
        settings.ProjectKey.Should().Be(projectKey);
        settings.DoneStatusName.Should().Be(doneStatus);
        settings.RejectStatusName.Should().Be(rejectStatus);
        settings.RequiredPathStages.Should().ContainInOrder(requiredPathStages);
        settings.MonthLabel.Should().Be(monthLabel);
        settings.CreatedAfter.Should().Be(createdAfter);
        settings.IssueTypes.Select(static issueType => issueType.Value).Should().ContainInOrder("Bug", "Story");
        settings.CustomFieldName.Should().Be(customFieldName);
        settings.CustomFieldValue.Should().Be(customFieldValue);
        settings.ShowTimeCalculationsInHoursOnly.Should().BeTrue();
        settings.ExcludeWeekend.Should().BeTrue();
        settings.ExcludedDays.Should().ContainInOrder(excludedDays);
        settings.BugIssueNames.Select(static issueType => issueType.Value).Should().ContainSingle("Bug");
        settings.ShowGeneralStatistics.Should().BeFalse();
        settings.ReleaseReport.Should().Be(releaseReport);
        settings.ArchTasksReport.Should().Be(archTasksReport);
        settings.GlobalIncidentsReport.Should().Be(globalIncidentsReport);
        settings.PdfReport.Should().Be(pdfReport);
        settings.PullRequestFieldName.Should().Be(pullRequestFieldName);
    }

    [Fact(DisplayName = "Constructor keeps pull request field name unset when value is not provided")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPullRequestFieldNameIsMissingKeepsNull()
    {
        // Act
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-02"));

        // Assert
        settings.ShowTimeCalculationsInHoursOnly.Should().BeFalse();
        settings.PullRequestFieldName.Should().BeNull();
    }
}
