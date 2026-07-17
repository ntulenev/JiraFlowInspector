using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JiraMetrics.Tests.Presentation.Pdf;

public sealed class PdfContentComposerTests
{
    [Fact(DisplayName = "ComposeContent throws when column is null")]
    [Trait("Category", "Unit")]
    public void ComposeContentWhenColumnIsNullThrowsArgumentNullException()
    {
        // Arrange
        var composer = new PdfContentComposer();
        var reportData = CreateReportData();

        // Act
        Action act = () => composer.ComposeContent(null!, reportData);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("column");
    }

    [Fact(DisplayName = "ComposeContent renders PDF for populated report data")]
    [Trait("Category", "Unit")]
    public void ComposeContentWhenReportDataIsPopulatedRendersPdf()
    {
        // Arrange
        var composer = new PdfContentComposer();
        var reportData = CreateReportData();

        // Act
        var bytes = GeneratePdf(column => composer.ComposeContent(column, reportData));

        // Assert
        bytes.Should().NotBeEmpty();
    }

    private static byte[] GeneratePdf(Action<ColumnDescriptor> composeContent)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document
            .Create(container =>
            {
                _ = container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(10);
                    page.Content().Column(composeContent);
                });
            })
            .GeneratePdf();
    }

    private static JiraReportData CreateReportData()
    {
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review"), new StageName("Release Candidate")],
            new MonthLabel("2026-02"),
            createdAfter: new CreatedAfterDate("2026-01-01"),
            issueTypes: [new IssueTypeName("Task"), new IssueTypeName("Bug")],
            customFieldName: "ADF Team",
            customFieldValue: "Processing",
            bugIssueNames: [new IssueTypeName("Bug")],
            internalIncidentIssueNames: [new IssueTypeName("Incident")],
            releaseReport: new ReleaseReportSettings(
                new ProjectKey("RLS"),
                "ADF",
                "Change completion date",
                environmentFieldName: "customfield_10865",
                environmentFieldValue: "P005"),
            archTasksReport: new ArchTasksReportSettings(
                "project = AAA AND type = \"Arch Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC"),
            globalIncidentsReport: new GlobalIncidentsReportSettings(
                namespaceName: "Incidents",
                jqlFilter: "(labels = SERVICE OR summary ~ \"SERVICE\") AND (summary ~ \"downtime\")",
                additionalFieldNames: ["Business Impact"]),
            pdfReport: new PdfReportSettings(true, "report.pdf"),
            customTransitionAnalysis: new CustomTransitionAnalysisSettings(
                new StatusName("Code Review"),
                new StatusName("Done")),
            unresolved30DaysTasksReport: new Unresolved30DaysTasksReportSettings(
                "project = AAA AND statusCategory != Done AND created <= -30d ORDER BY created ASC"));

        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Task summary"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            subItemsCount: 2,
            hasPullRequest: true);
        var summary = new PathGroupsSummary(new ItemCount(1), new ItemCount(1), new ItemCount(0), new ItemCount(1));
        var group = new PathGroup(
            issue.PathLabel,
            [issue],
            [new PercentileTransition(new StatusName("Open"), new StatusName("Code Review"), TimeSpan.FromHours(1))],
            TimeSpan.FromHours(1));

        return new JiraReportData
        {
            Settings = settings,
            Source = new JiraReportSourceData
            {
                SearchIssueCount = new ItemCount(1),
                ReleaseIssues =
                [
                    new ReleaseIssueItem(
                        new IssueKey("RLS-1"),
                        new IssueSummary("Release title"),
                        new DateOnly(2026, 2, 10),
                        tasks: 3,
                        components: 2,
                        environmentNames: ["P005", "S005"])
                ],
                ArchTasks =
                [
                    new ArchTaskItem(
                        new IssueKey("AAA-7"),
                        new IssueSummary("Architecture review"),
                        new DateTimeOffset(2026, 2, 2, 9, 30, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 2, 5, 12, 0, 0, TimeSpan.Zero))
                ],
                Unresolved30DaysTasks =
                [
                    new IssueListItem(
                        new IssueKey("AAA-30"),
                        new IssueSummary("Long-running story"),
                        new DateTimeOffset(2025, 12, 1, 9, 0, 0, TimeSpan.Zero),
                        issueType: "Story",
                        assignee: "Ada Lovelace",
                        status: "In Progress")
                ],
                GlobalIncidents =
                [
                    new GlobalIncidentItem(
                        new IssueKey("INC-1"),
                        new IssueSummary("ADF disabled"),
                        new DateTimeOffset(2026, 2, 12, 10, 0, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 2, 12, 10, 49, 0, TimeSpan.Zero),
                        impact: "Significant / Large",
                        urgency: "High",
                        additionalFields: new Dictionary<string, string?>
                        {
                            ["Business Impact"] = "Live feed unavailable"
                        })
                ],
                OpenIssuesByStatus =
                [
                    new StatusIssueTypeSummary(
                        new StatusName("QA"),
                        new ItemCount(2),
                        [
                            new IssueTypeCountSummary(new IssueTypeName("UserStory"), new ItemCount(1)),
                            new IssueTypeCountSummary(new IssueTypeName("SubTask"), new ItemCount(1))
                        ])
                ]
            },
            Ratios = new JiraReportRatioData
            {
                AllTasks = new IssueRatioSnapshot(
                    new ItemCount(47),
                    new ItemCount(33),
                    new ItemCount(31),
                    new ItemCount(37),
                    new ItemCount(68),
                    [],
                    [],
                    []),
                Bugs = new IssueRatioSnapshot(
                    new ItemCount(2),
                    new ItemCount(1),
                    new ItemCount(1),
                    new ItemCount(0),
                    new ItemCount(1),
                    [new IssueListItem(new IssueKey("AAA-2"), new IssueSummary("Open bug"))],
                    [new IssueListItem(new IssueKey("AAA-3"), new IssueSummary("Done bug"))],
                    []),
                InternalIncidents = new IssueRatioSnapshot(
                    new ItemCount(3),
                    new ItemCount(1),
                    new ItemCount(1),
                    new ItemCount(1),
                    new ItemCount(2),
                    [new IssueListItem(new IssueKey("AAA-4"), new IssueSummary("Open incident"))],
                    [new IssueListItem(new IssueKey("AAA-5"), new IssueSummary("Done incident"))],
                    [new IssueListItem(new IssueKey("AAA-6"), new IssueSummary("Rejected incident"))])
            },
            Transitions = new JiraReportTransitionData
            {
                DoneIssues = [issue],
                QaTransitionAnalysis = new QaTransitionAnalysis(
                new ItemCount(1),
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(
                            new StatusName("Quality Assurance"),
                            new StatusName("QA IN PROGRESS")),
                        DateTimeOffset.UtcNow.AddHours(-1),
                        TimeSpan.FromHours(2))
                ],
                TimeSpan.FromHours(2),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(2))],
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(
                            new StatusName("QA in progress"),
                            new StatusName("Release Candidate")),
                        DateTimeOffset.UtcNow,
                        TimeSpan.FromHours(4))
                ],
                TimeSpan.FromHours(4),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(4))],
                [
                    new TransitionMeasurementIssue(
                        issue,
                        new TransitionMeasurementRule(
                            new StatusName("QA on hold"),
                            new StatusName("QA IN PROGRESS")),
                        DateTimeOffset.UtcNow,
                        TimeSpan.FromHours(1))
                ],
                TimeSpan.FromHours(1),
                [new IssueTypeDuration75Summary(new IssueTypeName("Task"), new ItemCount(1), TimeSpan.FromHours(1))]),
                RejectedIssues = [],
                PathSummary = summary,
                PathGroups = [group]
            },
            Failures = []
        };
    }
}
