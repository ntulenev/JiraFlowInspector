using System.Diagnostics.CodeAnalysis;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

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
    public void RenderReport(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (!_settings.HtmlReport.Enabled)
        {
            return;
        }

        var outputPath = _settings.HtmlReport.ResolveOutputPath();
        var html = _htmlContentComposer.Compose(reportData);
        _htmlReportFileStore.Save(outputPath, html);

        Console.WriteLine($"HTML report saved to: {outputPath}");

        if (!_settings.HtmlReport.OpenAfterGeneration)
        {
            return;
        }

        try
        {
            _htmlReportLauncher.Open(outputPath);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            WriteHtmlOpenFailedMessage(outputPath, ex.Message);
        }
    }

    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "CLI warning message for local desktop usage.")]
    private static void WriteHtmlOpenFailedMessage(string outputPath, string reason)
    {
        Console.WriteLine($"Failed to open HTML automatically: {outputPath} ({reason})");
    }

    private readonly AppSettings _settings;
    private readonly IHtmlReportFileStore _htmlReportFileStore;
    private readonly IHtmlReportLauncher _htmlReportLauncher;
    private readonly IHtmlContentComposer _htmlContentComposer;
}
