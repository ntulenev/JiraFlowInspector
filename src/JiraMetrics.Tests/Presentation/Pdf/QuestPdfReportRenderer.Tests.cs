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
        var launcher = new Mock<IPdfReportLauncher>(MockBehavior.Strict).Object;
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict).Object;

        // Act
        Action act = () => _ = new QuestPdfReportRenderer(null!, fileStore, launcher, composer);

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
        var launcher = new Mock<IPdfReportLauncher>(MockBehavior.Strict).Object;
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict).Object;
        var renderer = new QuestPdfReportRenderer(options, fileStore, launcher, composer);

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
        var launcher = new Mock<IPdfReportLauncher>(MockBehavior.Strict);
        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict);
        var renderer = new QuestPdfReportRenderer(options, fileStore.Object, launcher.Object, composer.Object);

        // Act
        renderer.RenderReport(CreateReportData(options.Value));

        // Assert
        fileStore.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<IDocument>()), Times.Never);
        launcher.Verify(x => x.Open(It.IsAny<string>()), Times.Never);
        composer.Verify(
            x => x.ComposeContent(It.IsAny<QuestPDF.Fluent.ColumnDescriptor>(), It.IsAny<JiraPdfReportData>()),
            Times.Never);
    }

    [Fact(DisplayName = "RenderReport composes stores and opens PDF when enabled and auto-open is on")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenEnabledAndAutoOpenEnabledComposesStoresAndOpensPdf()
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
        var openCalls = 0;

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

        var launcher = new Mock<IPdfReportLauncher>(MockBehavior.Strict);
        launcher
            .Setup(x => x.Open(It.Is<string>(path => string.Equals(path, expectedPath, StringComparison.OrdinalIgnoreCase))))
            .Callback(() => openCalls++);

        var renderer = new QuestPdfReportRenderer(options, fileStore.Object, launcher.Object, composer.Object);

        // Act
        renderer.RenderReport(reportData);

        // Assert
        composeCalls.Should().Be(1);
        saveCalls.Should().Be(1);
        openCalls.Should().Be(1);
    }

    [Fact(DisplayName = "RenderReport stores PDF but does not open it when auto-open is disabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenAutoOpenDisabledStoresPdfWithoutOpeningIt()
    {
        // Arrange
        var settings = CreateSettings(
            pdfEnabled: true,
            outputPath: Path.Combine("reports", "result.pdf"),
            openAfterGeneration: false);
        var options = Options.Create(settings);
        var reportData = CreateReportData(settings);

        var composer = new Mock<IPdfContentComposer>(MockBehavior.Strict);
        composer
            .Setup(x => x.ComposeContent(
                It.IsAny<QuestPDF.Fluent.ColumnDescriptor>(),
                It.Is<JiraPdfReportData>(data => ReferenceEquals(data, reportData))));

        var fileStore = new Mock<IPdfReportFileStore>(MockBehavior.Strict);
        fileStore
            .Setup(x => x.Save(It.IsAny<string>(), It.IsAny<IDocument>()));

        var launcher = new Mock<IPdfReportLauncher>(MockBehavior.Strict);

        var renderer = new QuestPdfReportRenderer(options, fileStore.Object, launcher.Object, composer.Object);

        // Act
        renderer.RenderReport(reportData);

        // Assert
        fileStore.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<IDocument>()), Times.Once);
        launcher.Verify(x => x.Open(It.IsAny<string>()), Times.Never);
    }

    private static AppSettings CreateSettings(bool pdfEnabled, string outputPath = "report.pdf", bool openAfterGeneration = true)
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
            pdfReport: new PdfReportSettings(pdfEnabled, outputPath, openAfterGeneration));
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
