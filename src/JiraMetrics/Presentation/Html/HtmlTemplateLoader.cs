namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Loads embedded HTML templates used by the report composer.
/// </summary>
internal static class HtmlTemplateLoader
{
    /// <summary>
    /// Loads the report document template.
    /// </summary>
    /// <returns>Report template content.</returns>
    public static string LoadReportTemplate() => _reportTemplate.Value;

    /// <summary>
    /// Loads report document styles.
    /// </summary>
    /// <returns>Report stylesheet content.</returns>
    public static string LoadReportStyles() => _reportStyles.Value;

    /// <summary>
    /// Loads report document script.
    /// </summary>
    /// <returns>Report script content.</returns>
    public static string LoadReportScript() => _reportScript.Value;

    private static string LoadTemplate(string resourceName)
    {
        var assembly = typeof(HtmlTemplateLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded HTML resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static readonly Lazy<string> _reportTemplate = new(() => LoadTemplate("JiraMetrics.HtmlTemplates.ReportDocument.html"));

    private static readonly Lazy<string> _reportStyles = new(() => LoadTemplate("JiraMetrics.HtmlTemplates.ReportDocument.css"));

    private static readonly Lazy<string> _reportScript = new(() => LoadTemplate("JiraMetrics.HtmlTemplates.ReportDocument.js"));
}
