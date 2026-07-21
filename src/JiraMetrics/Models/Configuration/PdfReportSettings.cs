using System.Globalization;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated PDF report settings.
/// </summary>
public sealed record PdfReportSettings
{

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfReportSettings"/> class.
    /// </summary>
    /// <param name="enabled">Whether PDF generation is enabled.</param>
    /// <param name="openAfterGeneration">Whether generated PDF should be opened after save.</param>
    /// <param name="outputPath">Configured output path.</param>
    public PdfReportSettings(bool enabled = true, string? outputPath = null, bool openAfterGeneration = true)
    {
        Enabled = enabled;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? DEFAULT_OUTPUT_PATH : outputPath.Trim();
        OpenAfterGeneration = openAfterGeneration;
    }

    /// <summary>
    /// Gets a value indicating whether PDF generation is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether generated PDF should be opened after save.
    /// </summary>
    public bool OpenAfterGeneration { get; }

    /// <summary>
    /// Gets configured output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Resolves output path using the timestamp captured for the report run.
    /// </summary>
    /// <param name="generatedAt">Report generation timestamp.</param>
    /// <returns>Absolute dated output path.</returns>
    public string ResolveOutputPath(DateTimeOffset generatedAt) =>
        ResolveOutputPath(generatedAt, fileNamePrefix: null);

    /// <summary>
    /// Resolves output path using the report timestamp and an optional filename prefix.
    /// </summary>
    /// <param name="generatedAt">Report generation timestamp.</param>
    /// <param name="fileNamePrefix">Optional filename prefix.</param>
    /// <returns>Absolute dated output path.</returns>
    public string ResolveOutputPath(DateTimeOffset generatedAt, string? fileNamePrefix)
    {
        var candidatePath = string.IsNullOrWhiteSpace(OutputPath)
            ? DEFAULT_OUTPUT_PATH
            : OutputPath.Trim();

        var absolutePath = Path.IsPathRooted(candidatePath)
            ? Path.GetFullPath(candidatePath)
            : Path.GetFullPath(candidatePath, Directory.GetCurrentDirectory());

        return AppendDateSuffix(absolutePath, generatedAt, fileNamePrefix);
    }

    private static string AppendDateSuffix(string absolutePath, DateTimeOffset generatedAt, string? fileNamePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var directoryPath = Path.GetDirectoryName(absolutePath);
        var extension = Path.GetExtension(absolutePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
        if (!string.IsNullOrWhiteSpace(fileNamePrefix))
        {
            fileNameWithoutExtension = fileNamePrefix.Trim() + "_" + fileNameWithoutExtension;
        }

        var dateSuffix = generatedAt.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var datedFileName = fileNameWithoutExtension + "_" + dateSuffix + extension;

        return string.IsNullOrWhiteSpace(directoryPath)
            ? datedFileName
            : Path.Combine(directoryPath, datedFileName);
    }
    private const string DEFAULT_OUTPUT_PATH = "jiraflowinspector-report.pdf";
}
