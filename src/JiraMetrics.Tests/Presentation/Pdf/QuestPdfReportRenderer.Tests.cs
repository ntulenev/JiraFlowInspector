using System.Globalization;

using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Pdf;

using Microsoft.Extensions.Options;

using Moq;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace JiraMetrics.Tests.Presentation.Pdf;

public sealed class QuestPdfReportRendererTests
{
    [Fact(DisplayName = "Constructor throws when options are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOptionsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var fileStore = new Mock<IPdfReportFileStore>(MockBehavior.Strict).Object;
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new QuestPdfReportRenderer(null!, fileStore, composer);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact(DisplayName = "RenderReport validates null report data")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenReportDataIsNullThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(CreateSettings(pdfEnabled: false));
        var fileStore = new Mock<IPdfReportFileStore>(MockBehavior.Strict).Object;
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict).Object;
        var renderer = new QuestPdfReportRenderer(options, fileStore, composer);

        // Act
        Action act = () => renderer.RenderReport(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("reportData");
    }

    [Fact(DisplayName = "RenderReport skips rendering when PDF is disabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenDisabledSkipsRendering()
    {
        // Arrange
        var options = Options.Create(CreateSettings(pdfEnabled: false));
        var fileStore = new Mock<IPdfReportFileStore>(MockBehavior.Strict);
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict);
        var renderer = new QuestPdfReportRenderer(options, fileStore.Object, composer.Object);

        // Act
        renderer.RenderReport(CreateReportData(options.Value));

        // Assert
        fileStore.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<IDocument>()), Times.Never);
        composer.Verify(
            x => x.ComposeContent(It.IsAny<QuestPDF.Fluent.ColumnDescriptor>(), It.IsAny<JiraPdfReportData>()),
            Times.Never);
    }

    [Fact(DisplayName = "RenderReport composes and stores PDF when enabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenEnabledComposesAndStoresPdf()
    {
        // Arrange
        var settings = CreateSettings(
            pdfEnabled: true,
            outputPath: Path.Combine("reports", "result.pdf"));
        var options = Options.Create(settings);
        var reportData = CreateReportData(settings);
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expectedPath = Path.GetFullPath(
            Path.Combine("reports", $"result_{dateSuffix}.pdf"),
            Directory.GetCurrentDirectory());

        var composeCalls = 0;
        var saveCalls = 0;

        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict);
        composer
            .Setup(x => x.ComposeContent(
                It.Is<QuestPDF.Fluent.ColumnDescriptor>(column => column != null),
                It.Is<JiraPdfReportData>(data => ReferenceEquals(data, reportData))))
            .Callback(() => composeCalls++);

        var fileStore = new Mock<IPdfReportFileStore>(MockBehavior.Strict);
        fileStore
            .Setup(x => x.Save(
                It.Is<string>(path => string.Equals(path, expectedPath, StringComparison.OrdinalIgnoreCase)),
                It.Is<IDocument>(document => document != null)))
            .Callback<string, IDocument>((_, document) =>
            {
                saveCalls++;
                var bytes = document.GeneratePdf();
                bytes.Should().NotBeEmpty();
            });

        var renderer = new QuestPdfReportRenderer(options, fileStore.Object, composer.Object);

        // Act
        renderer.RenderReport(reportData);

        // Assert
        composeCalls.Should().Be(1);
        saveCalls.Should().Be(1);
    }

    private static AppSettings CreateSettings(bool pdfEnabled, string outputPath = "report.pdf")
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            createdAfter: null,
            issueTypes: [new IssueTypeName("Task"), new IssueTypeName("Bug")],
            customFieldName: "ADF Team",
            customFieldValue: "Processing",
            excludeWeekend: false,
            bugIssueNames: [new IssueTypeName("Bug")],
            pdfReport: new PdfReportSettings(pdfEnabled, outputPath));
    }

    private static JiraPdfReportData CreateReportData(AppSettings settings)
    {
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Task summary"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"),
            subItemsCount: 1,
            hasPullRequest: true);
        var summary = new PathGroupsSummary(
            new ItemCount(1),
            new ItemCount(1),
            new ItemCount(0),
            new ItemCount(1));
        var group = new PathGroup(
            issue.PathLabel,
            [issue],
            [new PercentileTransition(new StatusName("Open"), new StatusName("Done"), TimeSpan.FromHours(1))],
            TimeSpan.FromHours(1));

        return new JiraPdfReportData
        {
            Settings = settings,
            SearchIssueCount = new ItemCount(1),
            DoneIssues = [issue],
            RejectedIssues = [],
            PathSummary = summary,
            PathGroups = [group]
        };
    }
}
