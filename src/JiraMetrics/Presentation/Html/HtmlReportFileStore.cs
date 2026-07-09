using System.Text;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Filesystem-backed HTML report store.
/// </summary>
public sealed class HtmlReportFileStore : IHtmlReportFileStore
{
    /// <inheritdoc />
    public void Save(string outputPath, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(html);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
