using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Supported generated report formats.
/// </summary>
public enum ReportOutputFormat
{
    /// <summary>
    /// Standalone HTML report.
    /// </summary>
    Html = 0,

    /// <summary>
    /// PDF report.
    /// </summary>
    Pdf = 1
}

/// <summary>
/// Describes one generated report file and its optional auto-open failure.
/// </summary>
public sealed record ReportOutput
{
    /// <summary>
    /// Initializes a new report output result.
    /// </summary>
    /// <param name="format">Generated report format.</param>
    /// <param name="outputPath">Generated file path.</param>
    /// <param name="openFailure">Optional failure raised while opening the generated file.</param>
    public ReportOutput(
        ReportOutputFormat format,
        string outputPath,
        ErrorMessage? openFailure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        Format = format;
        OutputPath = outputPath;
        OpenFailure = openFailure;
    }

    /// <summary>
    /// Gets the generated report format.
    /// </summary>
    public ReportOutputFormat Format { get; }

    /// <summary>
    /// Gets the generated file path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets the optional failure raised while opening the generated file.
    /// </summary>
    public ErrorMessage? OpenFailure { get; }
}
