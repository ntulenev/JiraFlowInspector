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
        html.Should().Contain("report-nav");
        html.Should().Contain("href=\"#path-groups\"");
        html.Should().Contain("Issues moved to Done in selected period");
        html.Should().Contain("Path Groups Summary");
        html.Should().Contain("Transition Len");
        html.Should().Contain("data-toggle-detail");
        html.Should().Contain("3h (3h)");
        html.Should().Contain("Release Report");
        html.Should().Contain("Components Release Table");
        html.Should().Contain("Bug Ratio: Open Issues");
        html.Should().Contain("Automated Tests: Coverage");
        html.Should().Contain("50%");
        html.Should().Contain("QA Transition Analysis");
        html.Should().Contain("Testing time by issue");
        html.Should().Contain("General Statistics");
        html.Should().Contain("General Statistics is a current snapshot.");
        html.Should().Contain("It is not a historical period slice.");
        html.Should().Contain("Architecture Tasks");
        html.Should().Contain("Global Incidents");
        html.Should().Contain("Failed Issues");
        html.Should().Contain("data-table-panel");
        html.Should().Contain("data-sort-column=\"0\"");
        html.Should().Contain("Reset Filters");
        html.Should().Contain("browse/AAA-1");
        html.IndexOf("id=\"global-incidents\"", StringComparison.Ordinal).Should()
            .BeLessThan(html.IndexOf("id=\"ratios\"", StringComparison.Ordinal));
        html.IndexOf("id=\"general-statistics\"", StringComparison.Ordinal).Should()
            .BeLessThan(html.IndexOf("id=\"failures\"", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Compose renders empty-state text when report has no data")]
    [Trait("Category", "Unit")]
    public void ComposeWhenDataIsEmptyRendersEmptyStates()
    {
        // Arrange
        var composer = new HtmlContentComposer();
        var settings = CreateSettings();
        var reportData = new JiraReportData
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
        html.Should().NotContain("id=\"failures\"");
    }

    private static JiraReportData CreateReportData()
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

        return new JiraReportData
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
                    components: 1,
                    componentNames: ["Trading"],
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
            BugCreatedThisMonth = new ItemCount(2),
            BugMovedToDoneThisMonth = new ItemCount(1),
            BugRejectedThisMonth = new ItemCount(0),
            BugFinishedThisMonth = new ItemCount(1),
            BugReporducedOnProd = new ItemCount(1),
            BugOpenIssues =
            [
                new IssueListItem(
                    new IssueKey("AAA-2"),
                    new IssueSummary("Open prod bug"),
                    new DateTimeOffset(2026, 2, 8, 10, 0, 0, TimeSpan.Zero),
                    reporducedOnProd: true,
                    priority: "P1")
            ],
            BugDoneIssues =
            [
                new IssueListItem(
                    new IssueKey("AAA-3"),
                    new IssueSummary("Done bug"),
                    new DateTimeOffset(2026, 2, 7, 10, 0, 0, TimeSpan.Zero))
            ],
            BugRejectedIssues = [],
            TestCoverage = new TestCoverageSnapshot(
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Covered supertask")),
                new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Uncovered supertask"))
            ],
            [
                new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Covered supertask"))
            ]),
            DoneIssues = [issue],
            DoneDaysAtWork75PerType =
            [
                new IssueTypeWorkDays75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(3))
            ],
            QaTransitionAnalysis = new QaTransitionAnalysis(
                new ItemCount(1),
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(new StatusName("Open"), new StatusName("Done")),
                        transition.At,
                        transition.SincePrevious)
                ],
                TimeSpan.FromHours(3),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(3))],
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(new StatusName("Open"), new StatusName("Done")),
                        transition.At,
                        transition.SincePrevious)
                ],
                TimeSpan.FromHours(3),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(3))],
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(new StatusName("QA on hold"), new StatusName("QA IN PROGRESS")),
                        transition.At,
                        transition.SincePrevious)
                ],
                TimeSpan.FromHours(3),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(3))]),
            OpenIssuesByStatus =
            [
                new StatusIssueTypeSummary(
                    new StatusName("In Progress"),
                    new ItemCount(1),
                    [new IssueTypeCountSummary(new IssueTypeName("Task"), new ItemCount(1))])
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
            testCoverage: new TestCoverageSettings(
                issueTypes: [new IssueTypeName("SuperTask")],
                testProjectKey: new ProjectKey("QA"),
                linkName: "is tested by"),
            releaseReport: new ReleaseReportSettings(
                new ProjectKey("RLS"),
                "ADF",
                "Release date",
                componentsFieldName: "Components"),
            archTasksReport: new ArchTasksReportSettings("project = AAA"),
            globalIncidentsReport: new GlobalIncidentsReportSettings(jqlFilter: "labels = INCIDENT"));
}
