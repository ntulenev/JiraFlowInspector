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

    private static string LoadTemplate(string resourceName)
    {
        var assembly = typeof(HtmlTemplateLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded HTML template '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static readonly Lazy<string> _reportTemplate = new(() => LoadTemplate("JiraMetrics.HtmlTemplates.ReportDocument.html"));
}
