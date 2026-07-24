using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// HTML implementation for Jira report rendering.
/// </summary>
public sealed class HtmlReportRenderer : IHtmlReportRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlReportRenderer"/> class.
    /// </summary>
    /// <param name="options">Application settings options.</param>
    /// <param name="htmlReportFileStore">HTML output file store.</param>
    /// <param name="htmlReportLauncher">HTML report launcher.</param>
    /// <param name="htmlContentComposer">HTML content composer.</param>
    public HtmlReportRenderer(
        IOptions<AppSettings> options,
        IHtmlReportFileStore htmlReportFileStore,
        IHtmlReportLauncher htmlReportLauncher,
        IHtmlContentComposer htmlContentComposer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(htmlReportFileStore);
        ArgumentNullException.ThrowIfNull(htmlReportLauncher);
        ArgumentNullException.ThrowIfNull(htmlContentComposer);

        _settings = options.Value;
        _htmlReportFileStore = htmlReportFileStore;
        _htmlReportLauncher = htmlReportLauncher;
        _htmlContentComposer = htmlContentComposer;
    }

    /// <inheritdoc />
    public IReadOnlyList<ReportOutput> RenderReport(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (!_settings.HtmlReport.Enabled)
        {
            return [];
        }

        var outputPath = _settings.HtmlReport.ResolveOutputPath(reportData.RunContext.GeneratedAt);
        var html = _htmlContentComposer.Compose(reportData);
        _htmlReportFileStore.Save(outputPath, html);

        if (!_settings.HtmlReport.OpenAfterGeneration)
        {
            return [new ReportOutput(ReportOutputFormat.Html, outputPath)];
        }

        ErrorMessage? openFailure = null;
        try
        {
            _htmlReportLauncher.Open(outputPath);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            openFailure = ErrorMessage.FromException(ex);
        }

        return [new ReportOutput(ReportOutputFormat.Html, outputPath, openFailure)];
    }

    private readonly AppSettings _settings;
    private readonly IHtmlReportFileStore _htmlReportFileStore;
    private readonly IHtmlReportLauncher _htmlReportLauncher;
    private readonly IHtmlContentComposer _htmlContentComposer;
}
