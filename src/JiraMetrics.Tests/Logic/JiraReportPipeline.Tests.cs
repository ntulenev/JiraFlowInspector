using FluentAssertions;

using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraReportPipelineTests
{
    [Fact(DisplayName = "Constructor rejects null dependencies")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenDependencyIsNullThrowsArgumentNullException()
    {
        var presenter = new Mock<IReportOutputPresenter>(MockBehavior.Strict).Object;

        Action nullRenderers = () => _ = new JiraReportPipeline(null!, presenter);
        Action nullPresenter = () => _ = new JiraReportPipeline([], null!);

        nullRenderers.Should().Throw<ArgumentNullException>();
        nullPresenter.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RenderReport invokes every renderer and presents generated outputs")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenCalledInvokesEveryRendererAndPresentsOutputs()
    {
        var reportData = CreateReportData();
        var htmlOutput = new ReportOutput(ReportOutputFormat.Html, "report.html");
        var openFailure = new ErrorMessage("No associated PDF viewer.");
        var pdfOutput = new ReportOutput(ReportOutputFormat.Pdf, "report.pdf", openFailure);
        var htmlRenderer = new Mock<IReportRenderer>(MockBehavior.Strict);
        var pdfRenderer = new Mock<IReportRenderer>(MockBehavior.Strict);
        var presenter = new Mock<IReportOutputPresenter>(MockBehavior.Strict);
        htmlRenderer.Setup(renderer => renderer.RenderReport(reportData)).Returns([htmlOutput]);
        pdfRenderer.Setup(renderer => renderer.RenderReport(reportData)).Returns([pdfOutput]);
        presenter.Setup(x => x.ShowReportSaved(ReportOutputFormat.Html, htmlOutput.OutputPath));
        presenter.Setup(x => x.ShowReportSaved(ReportOutputFormat.Pdf, pdfOutput.OutputPath));
        presenter.Setup(x => x.ShowReportOpenFailed(
            ReportOutputFormat.Pdf,
            pdfOutput.OutputPath,
            openFailure));
        var pipeline = new JiraReportPipeline(
            [htmlRenderer.Object, pdfRenderer.Object],
            presenter.Object);

        pipeline.RenderReport(reportData);

        htmlRenderer.Verify(renderer => renderer.RenderReport(reportData), Times.Once);
        pdfRenderer.Verify(renderer => renderer.RenderReport(reportData), Times.Once);
        presenter.Verify(x => x.ShowReportSaved(It.IsAny<ReportOutputFormat>(), It.IsAny<string>()), Times.Exactly(2));
        presenter.Verify(x => x.ShowReportOpenFailed(
            It.IsAny<ReportOutputFormat>(),
            It.IsAny<string>(),
            It.IsAny<ErrorMessage>()), Times.Once);
    }

    [Fact(DisplayName = "RenderReport ignores disabled renderer outputs")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenRendererReturnsNoOutputsDoesNotPresentAnything()
    {
        var reportData = CreateReportData();
        var renderer = new Mock<IReportRenderer>(MockBehavior.Strict);
        var presenter = new Mock<IReportOutputPresenter>(MockBehavior.Strict);
        renderer.Setup(x => x.RenderReport(reportData)).Returns([]);
        var pipeline = new JiraReportPipeline([renderer.Object], presenter.Object);

        pipeline.RenderReport(reportData);

        presenter.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "RenderReport rejects null report data")]
    [Trait("Category", "Unit")]
    public void RenderReportWhenReportDataIsNullThrowsArgumentNullException()
    {
        var pipeline = new JiraReportPipeline(
            [],
            new Mock<IReportOutputPresenter>(MockBehavior.Strict).Object);

        Action act = () => pipeline.RenderReport(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static JiraReportData CreateReportData() =>
        new()
        {
            RunContext = new ReportRunContext(
                new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2))),
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
