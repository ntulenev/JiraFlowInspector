using FluentAssertions;

using JiraMetrics.Abstractions.Html;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraReportPipelineTests
{
    [Fact(DisplayName = "Constructor rejects null report renderers")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenRendererIsNullThrowsArgumentNullException()
    {
        var htmlRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object;
        var pdfRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object;

        Action nullHtmlRenderer = () => _ = new JiraReportPipeline(null!, pdfRenderer);
        Action nullPdfRenderer = () => _ = new JiraReportPipeline(htmlRenderer, null!);

        nullHtmlRenderer.Should().Throw<ArgumentNullException>();
        nullPdfRenderer.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RenderReport invokes HTML and PDF renderers")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenCalledInvokesEveryRenderer()
    {
        var reportData = CreateReportData();
        var htmlRenderer = new Mock<IHtmlReportRenderer>(MockBehavior.Strict);
        var pdfRenderer = new Mock<IPdfReportRenderer>(MockBehavior.Strict);
        htmlRenderer.Setup(renderer => renderer.RenderReport(reportData));
        pdfRenderer.Setup(renderer => renderer.RenderReport(reportData));
        var pipeline = new JiraReportPipeline(htmlRenderer.Object, pdfRenderer.Object);

        pipeline.RenderReport(reportData);

        htmlRenderer.Verify(renderer => renderer.RenderReport(reportData), Times.Once);
        pdfRenderer.Verify(renderer => renderer.RenderReport(reportData), Times.Once);
    }

    [Fact(DisplayName = "RenderReport rejects null report data")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenReportDataIsNullThrowsArgumentNullException()
    {
        var pipeline = new JiraReportPipeline(
            new Mock<IHtmlReportRenderer>(MockBehavior.Strict).Object,
            new Mock<IPdfReportRenderer>(MockBehavior.Strict).Object);

        Action act = () => pipeline.RenderReport(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static JiraReportData CreateReportData() =>
        new()
        {
            Settings = new AppSettings(
                new JiraBaseUrl("https://example.atlassian.net"),
                new JiraEmail("user@example.test"),
                new JiraApiToken("token"),
                new ProjectKey("APP"),
                new StatusName("Done"),
                rejectStatusName: null,
                requiredPathStages: [],
                monthLabel: new MonthLabel("2026-07"))
        };
}
