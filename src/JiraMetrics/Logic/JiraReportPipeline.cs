using JiraMetrics.Models;

namespace JiraMetrics.Logic;

/// <summary>
/// Generates the configured HTML and PDF report outputs.
/// </summary>
internal sealed class JiraReportPipeline : IJiraReportPipeline
{
    public JiraReportPipeline(
        IHtmlReportRenderer htmlReportRenderer,
        IPdfReportRenderer pdfReportRenderer)
    {
        ArgumentNullException.ThrowIfNull(htmlReportRenderer);
        ArgumentNullException.ThrowIfNull(pdfReportRenderer);

        _htmlReportRenderer = htmlReportRenderer;
        _pdfReportRenderer = pdfReportRenderer;
    }

    public void RenderReport(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        _htmlReportRenderer.RenderReport(reportData);
        _pdfReportRenderer.RenderReport(reportData);
    }

    private readonly IHtmlReportRenderer _htmlReportRenderer;
    private readonly IPdfReportRenderer _pdfReportRenderer;
}
