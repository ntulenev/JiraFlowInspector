namespace JiraMetrics.Abstractions.Html;

/// <summary>
/// Persists generated HTML reports.
/// </summary>
public interface IHtmlReportFileStore
{
    /// <summary>
    /// Saves generated HTML to the provided output path.
    /// </summary>
    /// <param name="outputPath">Resolved output path.</param>
    /// <param name="html">Standalone HTML document.</param>
    void Save(string outputPath, string html);
}
