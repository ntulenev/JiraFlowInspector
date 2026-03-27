namespace JiraMetrics.Abstractions.Pdf;

/// <summary>
/// Opens generated PDF reports in the system default application.
/// </summary>
public interface IPdfReportLauncher
{
    /// <summary>
    /// Opens generated PDF report.
    /// </summary>
    /// <param name="pdfPath">Absolute PDF report path.</param>
    void Open(string pdfPath);
}

