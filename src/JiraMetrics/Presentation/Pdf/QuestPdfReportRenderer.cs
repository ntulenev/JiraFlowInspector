using System.Globalization;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.Options;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

using QLicenseType = QuestPDF.Infrastructure.LicenseType;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// QuestPDF implementation for Jira report rendering.
/// </summary>
public sealed class QuestPdfReportRenderer : IPdfReportRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuestPdfReportRenderer"/> class.
    /// </summary>
    /// <param name="options">Application settings options.</param>
    /// <param name="pdfReportFileStore">PDF output file store.</param>
    /// <param name="pdfContentComposer">PDF content composer.</param>
    public QuestPdfReportRenderer(
        IOptions<AppSettings> options,
        IPdfReportFileStore pdfReportFileStore,
        IPdfContentComposer pdfContentComposer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(pdfReportFileStore);
        ArgumentNullException.ThrowIfNull(pdfContentComposer);

        _settings = options.Value;
        _pdfReportFileStore = pdfReportFileStore;
        _pdfContentComposer = pdfContentComposer;
    }

    /// <inheritdoc />
    public void RenderReport(JiraPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (!_settings.PdfReport.Enabled)
        {
            return;
        }

        var outputPath = _settings.PdfReport.ResolveOutputPath();

        QuestPDF.Settings.License = QLicenseType.Community;

        var document = Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(static style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Spacing(2);
                    _ = column.Item().Text("Jira Analytics").Bold().FontSize(16);
                    _ = column.Item().Text(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Generated: {0:yyyy-MM-dd HH:mm:ss zzz}",
                            DateTimeOffset.Now));
                    _ = column.Item().Text(
                        "Project: "
                        + reportData.Settings.ProjectKey.Value
                        + "    Done status: "
                        + reportData.Settings.DoneStatusName.Value);
                    _ = column.Item().Text("Month: " + reportData.Settings.MonthLabel.Value);
                    if (reportData.Settings.CreatedAfter is { } createdAfter)
                    {
                        _ = column.Item().Text("Created after: " + createdAfter);
                    }

                    if (!string.IsNullOrWhiteSpace(reportData.Settings.CustomFieldName)
                        && !string.IsNullOrWhiteSpace(reportData.Settings.CustomFieldValue))
                    {
                        _ = column.Item().Text(
                            "Filtered by: "
                            + reportData.Settings.CustomFieldName
                            + " = "
                            + reportData.Settings.CustomFieldValue);
                    }
                });

                page.Content().PaddingTop(8).Column(column =>
                    _pdfContentComposer.ComposeContent(column, reportData));

                page.Footer().AlignRight().Text(text =>
                {
                    _ = text.Span("Page ");
                    _ = text.CurrentPageNumber();
                    _ = text.Span(" / ");
                    _ = text.TotalPages();
                });
            });
        });

        _pdfReportFileStore.Save(outputPath, document);

        System.Console.WriteLine($"PDF report saved to: {outputPath}");
    }

    private readonly AppSettings _settings;
    private readonly IPdfReportFileStore _pdfReportFileStore;
    private readonly IPdfContentComposer _pdfContentComposer;
}
