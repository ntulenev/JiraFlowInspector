using System.Diagnostics;


namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Opens generated PDF reports via the operating system shell.
/// </summary>
public sealed class PdfReportLauncher : IPdfReportLauncher
{
    /// <inheritdoc />
    public void Open(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = pdfPath,
            UseShellExecute = true
        });
    }
}

