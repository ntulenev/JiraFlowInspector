using FluentAssertions;

using JiraMetrics.Abstractions.Html;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Html;

using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.Presentation.Html;

public sealed class HtmlReportRendererTests
{
    [Fact(DisplayName = "RenderReport skips rendering when HTML is disabled")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenDisabledSkipsRendering()
    {
        // Arrange
        var options = Options.Create(CreateSettings(htmlEnabled: false));
        var fileStore = new Mock<IHtmlReportFileStore>(MockBehavior.Strict);
        var launcher = new Mock<IHtmlReportLauncher>(MockBehavior.Strict);
        var composer = new Mock<IHtmlContentComposer>(MockBehavior.Strict);
        var renderer = new HtmlReportRenderer(options, fileStore.Object, launcher.Object, composer.Object);

        // Act
        renderer.RenderReport(CreateReportData(options.Value));

        // Assert
        fileStore.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        launcher.Verify(x => x.Open(It.IsAny<string>()), Times.Never);
        composer.Verify(x => x.Compose(It.IsAny<JiraReportData>()), Times.Never);
    }

    [Fact(DisplayName = "RenderReport saves HTML and opens browser when configured")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenOpenAfterGenerationEnabledSavesAndLaunches()
    {
        // Arrange
        var settings = CreateSettings(htmlEnabled: true, openAfterGeneration: true);
        var options = Options.Create(settings);
        var reportData = CreateReportData(settings);
        string? savedPath = null;

        var fileStore = new Mock<IHtmlReportFileStore>(MockBehavior.Strict);
        fileStore
            .Setup(x => x.Save(
                It.Is<string>(path => Path.GetFileName(path).StartsWith("report_", StringComparison.OrdinalIgnoreCase)),
                "<html>report</html>"))
            .Callback<string, string>((path, _) => savedPath = path);

        var launcher = new Mock<IHtmlReportLauncher>(MockBehavior.Strict);
        launcher.Setup(x => x.Open(It.Is<string>(path => path == savedPath)));

        var composer = new Mock<IHtmlContentComposer>(MockBehavior.Strict);
        composer.Setup(x => x.Compose(It.Is<JiraReportData>(data => ReferenceEquals(data, reportData))))
            .Returns("<html>report</html>");

        var renderer = new HtmlReportRenderer(options, fileStore.Object, launcher.Object, composer.Object);

        // Act
        renderer.RenderReport(reportData);

        // Assert
        savedPath.Should().NotBeNullOrWhiteSpace();
    }

    private static AppSettings CreateSettings(bool htmlEnabled, bool openAfterGeneration = false) =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            htmlReport: new HtmlReportSettings(htmlEnabled, "report.html", openAfterGeneration));

    private static JiraReportData CreateReportData(AppSettings settings) =>
        new()
        {
            Settings = settings,
            Source = new JiraReportSourceData { SearchIssueCount = new ItemCount(1) },
            Transitions = new JiraReportTransitionData
            {
                PathSummary = new PathGroupsSummary(
                    new ItemCount(1),
                    new ItemCount(1),
                    new ItemCount(0),
                    new ItemCount(0))
            }
        };
}
