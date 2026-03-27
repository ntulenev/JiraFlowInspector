using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class AppSettingsFactoryTests
{
    [Fact(DisplayName = "Create maps Jira options to normalized app settings")]
    [Trait("Category", "Unit")]
    public void CreateWhenOptionsAreValidBuildsNormalizedSettings()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net"),
            Email = "user@example.com",
            ApiToken = "token",
            MonthLabel = "2026-03",
            CreatedAfter = "2026-03-01",
            ShowTimeCalculationsInHoursOnly = true,
            PullRequestFieldName = " customfield_22222 ",
            TeamTasks = new TeamTasksOptions
            {
                ProjectKey = "AAA",
                DoneStatusName = "Done",
                RejectStatusName = " Rejected ",
                CustomFieldName = " Team ",
                CustomFieldValue = " Import ",
                ShowGeneralStatistics = false,
                IssueTransitions = new IssueTransitionsOptions
                {
                    RequiredPathStages = [" Code Review ", "QA", "code review"],
                    IssueTypes = [" Story ", "Bug", "story"],
                    ExcludeWeekend = true,
                    ExcludedDays = ["01.03.2026", "2026-03-02", "01.03.2026"]
                },
                BugRatio = new BugRatioOptions
                {
                    BugIssueNames = [" Bug ", "Incident", "bug"]
                }
            },
            ReleaseReport = new ReleaseReportOptions
            {
                ReleaseProjectKey = "RLS",
                ProjectLabel = "Processing",
                ReleaseDateFieldName = "Change completion date"
            },
            ArchTasks = new ArchTasksReportOptions
            {
                Jql = "project = AAA"
            },
            GlobalIncidents = new GlobalIncidentsReportOptions
            {
                SearchPhrase = "ORX"
            },
            Pdf = new PdfOptions
            {
                Enabled = true,
                OutputPath = "report.pdf",
                OpenAfterGeneration = true
            }
        };

        // Act
        var settings = AppSettingsFactory.Create(options);

        // Assert
        settings.RequiredPathStages.Select(static stage => stage.Value)
            .Should().Equal("Code Review", "QA");
        settings.IssueTypes.Select(static issueType => issueType.Value)
            .Should().Equal("Story", "Bug");
        settings.BugIssueNames.Select(static issueType => issueType.Value)
            .Should().Equal("Bug", "Incident");
        settings.RejectStatusName!.Value.Value.Should().Be("Rejected");
        settings.CustomFieldName.Should().Be("Team");
        settings.CustomFieldValue.Should().Be("Import");
        settings.ExcludedDays.Should().Equal(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2));
        settings.ReportPeriod.MonthLabel!.Value.Value.Should().Be("2026-03");
        settings.PullRequestFieldName.Should().Be("customfield_22222");
        settings.ReleaseReport.Should().NotBeNull();
        settings.ArchTasksReport.Should().NotBeNull();
        settings.GlobalIncidentsReport.Should().NotBeNull();
        settings.PdfReport.Enabled.Should().BeTrue();
        settings.PdfReport.OpenAfterGeneration.Should().BeTrue();
    }

    [Fact(DisplayName = "Create requires at least one required path stage")]
    [Trait("Category", "Unit")]
    public void CreateWhenRequiredPathStagesAreMissingThrows()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net"),
            Email = "user@example.com",
            ApiToken = "token",
            TeamTasks = new TeamTasksOptions
            {
                ProjectKey = "AAA",
                DoneStatusName = "Done",
                IssueTransitions = new IssueTransitionsOptions
                {
                    RequiredPathStages = [" ", ""]
                }
            }
        };

        // Act
        var action = () => AppSettingsFactory.Create(options);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("At least one TeamTasks:IssueTransitions:RequiredPathStages entry must be configured.");
    }

    [Fact(DisplayName = "Create requires release report core fields when release report is configured")]
    [Trait("Category", "Unit")]
    public void CreateWhenReleaseReportIsIncompleteThrows()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net"),
            Email = "user@example.com",
            ApiToken = "token",
            TeamTasks = new TeamTasksOptions
            {
                ProjectKey = "AAA",
                DoneStatusName = "Done",
                IssueTransitions = new IssueTransitionsOptions
                {
                    RequiredPathStages = ["Code Review"]
                }
            },
            ReleaseReport = new ReleaseReportOptions
            {
                ProjectLabel = "Processing"
            }
        };

        // Act
        var action = () => AppSettingsFactory.Create(options);

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("ReleaseReport requires ReleaseProjectKey, ProjectLabel, and ReleaseDateFieldName when configured.");
    }
}
