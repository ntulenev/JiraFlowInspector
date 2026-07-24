using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Presentation;

/// <summary>
/// Presents report output generation results.
/// </summary>
public interface IReportOutputPresenter
{
    /// <summary>
    /// Shows a successfully generated report output.
    /// </summary>
    /// <param name="format">Report output format.</param>
    /// <param name="outputPath">Generated file path.</param>
    void ShowReportSaved(ReportOutputFormat format, string outputPath);

    /// <summary>
    /// Shows a failure to open a generated report automatically.
    /// </summary>
    /// <param name="format">Report output format.</param>
    /// <param name="outputPath">Generated file path.</param>
    /// <param name="errorMessage">Open failure details.</param>
    void ShowReportOpenFailed(
        ReportOutputFormat format,
        string outputPath,
        ErrorMessage errorMessage);
}
