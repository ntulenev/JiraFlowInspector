using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Html;

namespace JiraMetrics.Tests.Presentation.Html;

public sealed class HtmlContentComposerTests
{
    [Fact(DisplayName = "Compose renders interactive Jira report sections")]
    [Trait("Category", "Unit")]
    public void ComposeWhenDataExistsRendersExpectedSectionsAndControls()
    {
        // Arrange
        var composer = new HtmlContentComposer();
        var reportData = CreateReportData();

        // Act
        var html = composer.Compose(reportData);

        // Assert
        html.Should().Contain("JiraFlowInspector HTML Report");
        html.Should().Contain("Issues moved to Done in selected period");
        html.Should().Contain("Path Groups Summary");
        html.Should().Contain("Release Report");
        html.Should().Contain("Architecture Tasks");
        html.Should().Contain("Global Incidents");
        html.Should().Contain("Failed Issues");
        html.Should().Contain("data-table-panel");
        html.Should().Contain("data-sort-column=\"0\"");
        html.Should().Contain("Reset Filters");
        html.Should().Contain("browse/AAA-1");
    }

    [Fact(DisplayName = "Compose renders empty-state text when report has no data")]
    [Trait("Category", "Unit")]
    public void ComposeWhenDataIsEmptyRendersEmptyStates()
    {
        // Arrange
        var composer = new HtmlContentComposer();
        var settings = CreateSettings();
        var reportData = new JiraPdfReportData
        {
            Settings = settings,
            SearchIssueCount = new ItemCount(0),
            PathSummary = new PathGroupsSummary(
                new ItemCount(0),
                new ItemCount(0),
                new ItemCount(0),
                new ItemCount(0))
        };

        // Act
        var html = composer.Compose(reportData);

        // Assert
        html.Should().Contain("No issues.");
        html.Should().Contain("No path groups.");
        html.Should().Contain("No failed issue loads.");
    }

    private static JiraPdfReportData CreateReportData()
    {
        var settings = CreateSettings();
        var transition = new TransitionEvent(
            new StatusName("Open"),
            new StatusName("Done"),
            new DateTimeOffset(2026, 2, 10, 12, 0, 0, TimeSpan.Zero),
            TimeSpan.FromHours(3));
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Task summary"),
            new DateTimeOffset(2026, 2, 9, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 10, 12, 0, 0, TimeSpan.Zero),
            [transition],
            PathKey.FromTransitions([transition]),
            PathLabel.FromTransitions([transition]),
            subItemsCount: 2,
            hasPullRequest: true);

        return new JiraPdfReportData
        {
            Settings = settings,
            SearchIssueCount = new ItemCount(1),
            ReleaseIssues =
            [
                new ReleaseIssueItem(
                    new IssueKey("RLS-1"),
                    new IssueSummary("Release title"),
                    new DateOnly(2026, 2, 10),
                    tasks: 3,
                    environmentNames: ["P005"])
            ],
            ArchTasks =
            [
                new ArchTaskItem(
                    new IssueKey("AAA-7"),
                    new IssueSummary("Architecture review"),
                    new DateTimeOffset(2026, 2, 2, 9, 30, 0, TimeSpan.Zero))
            ],
            GlobalIncidents =
            [
                new GlobalIncidentItem(
                    new IssueKey("INC-1"),
                    new IssueSummary("ADF disabled"),
                    new DateTimeOffset(2026, 2, 12, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 2, 12, 10, 49, 0, TimeSpan.Zero),
                    impact: "High",
                    urgency: "Major")
            ],
            AllTasksCreatedThisMonth = new ItemCount(4),
            AllTasksOpenThisMonth = new ItemCount(1),
            AllTasksMovedToDoneThisMonth = new ItemCount(2),
            AllTasksRejectedThisMonth = new ItemCount(1),
            AllTasksFinishedThisMonth = new ItemCount(3),
            DoneIssues = [issue],
            DoneDaysAtWork75PerType =
            [
                new IssueTypeWorkDays75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(3))
            ],
            PathSummary = new PathGroupsSummary(
                new ItemCount(1),
                new ItemCount(1),
                new ItemCount(1),
                new ItemCount(1)),
            PathGroups =
            [
                new PathGroup(
                    issue.PathLabel,
                    [issue],
                    [new PercentileTransition(new StatusName("Open"), new StatusName("Done"), TimeSpan.FromHours(3))],
                    TimeSpan.FromHours(3))
            ],
            Failures = [new LoadFailure(new IssueKey("AAA-9"), new ErrorMessage("Timeout"))]
        };
    }

    private static AppSettings CreateSettings() =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            releaseReport: new ReleaseReportSettings(new ProjectKey("RLS"), "ADF", "Release date"),
            archTasksReport: new ArchTasksReportSettings("project = AAA"),
            globalIncidentsReport: new GlobalIncidentsReportSettings(jqlFilter: "labels = INCIDENT"));
}
