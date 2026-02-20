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

    private static JiraPdfReportData CreateReportData()
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
            releaseReport: new ReleaseReportSettings(
                new ProjectKey("RLS"),
                "ADF",
                "Change completion date"),
            pdfReport: new PdfReportSettings(true, "report.pdf"));

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
                    components: 2)
            ],
            BugCreatedThisMonth = new ItemCount(2),
            BugMovedToDoneThisMonth = new ItemCount(1),
            BugRejectedThisMonth = new ItemCount(0),
            BugFinishedThisMonth = new ItemCount(1),
            BugOpenIssues = [new IssueListItem(new IssueKey("AAA-2"), new IssueSummary("Open bug"))],
            BugDoneIssues = [new IssueListItem(new IssueKey("AAA-3"), new IssueSummary("Done bug"))],
            BugRejectedIssues = [],
            DoneIssues = [issue],
            RejectedIssues = [],
            PathSummary = summary,
            PathGroups = [group],
            Failures = []
        };
    }
}
