using System.Globalization;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated HTML report settings.
/// </summary>
public sealed record HtmlReportSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlReportSettings"/> class.
    /// </summary>
    /// <param name="enabled">Whether HTML generation is enabled.</param>
    /// <param name="outputPath">Configured output path.</param>
    /// <param name="openAfterGeneration">Whether generated HTML should be opened after save.</param>
    public HtmlReportSettings(bool enabled = true, string? outputPath = null, bool openAfterGeneration = false)
    {
        Enabled = enabled;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? DEFAULT_OUTPUT_PATH : outputPath.Trim();
        OpenAfterGeneration = openAfterGeneration;
    }

    /// <summary>
    /// Gets a value indicating whether HTML generation is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets configured output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets a value indicating whether generated HTML should be opened after save.
    /// </summary>
    public bool OpenAfterGeneration { get; }

    /// <summary>
    /// Resolves output path to absolute path and appends date suffix.
    /// </summary>
    /// <returns>Absolute dated output path.</returns>
    public string ResolveOutputPath() => ResolveOutputPath(fileNamePrefix: null);

    /// <summary>
    /// Resolves output path to absolute path, prepends an optional filename prefix, and appends date suffix.
    /// </summary>
    /// <param name="fileNamePrefix">Optional filename prefix.</param>
    /// <returns>Absolute dated output path.</returns>
    public string ResolveOutputPath(string? fileNamePrefix)
    {
        var candidatePath = string.IsNullOrWhiteSpace(OutputPath)
            ? DEFAULT_OUTPUT_PATH
            : OutputPath.Trim();

        var absolutePath = Path.IsPathRooted(candidatePath)
            ? Path.GetFullPath(candidatePath)
            : Path.GetFullPath(candidatePath, Directory.GetCurrentDirectory());

        return AppendDateSuffix(absolutePath, DateTime.Now, fileNamePrefix);
    }

    private static string AppendDateSuffix(string absolutePath, DateTime currentDate, string? fileNamePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var directoryPath = Path.GetDirectoryName(absolutePath);
        var extension = Path.GetExtension(absolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".html";
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
        if (!string.IsNullOrWhiteSpace(fileNamePrefix))
        {
            fileNameWithoutExtension = fileNamePrefix.Trim() + "_" + fileNameWithoutExtension;
        }

        var dateSuffix = currentDate.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var datedFileName = fileNameWithoutExtension + "_" + dateSuffix + extension;

        return string.IsNullOrWhiteSpace(directoryPath)
            ? datedFileName
            : Path.Combine(directoryPath, datedFileName);
    }

    private const string DEFAULT_OUTPUT_PATH = "jiraflowinspector-report.html";
}
