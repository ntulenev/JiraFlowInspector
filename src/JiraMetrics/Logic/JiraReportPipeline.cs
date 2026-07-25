using System.Diagnostics.CodeAnalysis;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Generates and presents all configured report outputs.
/// </summary>
internal sealed class JiraReportPipeline : IJiraReportPipeline
{
    public JiraReportPipeline(
        IEnumerable<IReportRenderer> renderers,
        IReportOutputPresenter outputPresenter)
    {
        ArgumentNullException.ThrowIfNull(renderers);
        ArgumentNullException.ThrowIfNull(outputPresenter);

        _renderers = [.. renderers];
        _outputPresenter = outputPresenter;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A report renderer is an output boundary; one format failure must not prevent the remaining formats from being generated.")]
    public void RenderReport(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        foreach (var renderer in _renderers)
        {
            IReadOnlyList<ReportOutput> outputs;
            try
            {
                outputs = renderer.RenderReport(reportData);
            }
            catch (Exception ex)
            {
                _outputPresenter.ShowReportGenerationFailed(
                    renderer.Format,
                    ErrorMessage.FromException(ex));
                continue;
            }

            foreach (var output in outputs)
            {
                _outputPresenter.ShowReportSaved(output.Format, output.OutputPath);
                if (output.OpenFailure is { } openFailure)
                {
                    _outputPresenter.ShowReportOpenFailed(
                        output.Format,
                        output.OutputPath,
                        openFailure);
                }
            }
        }
    }

    private readonly IReadOnlyList<IReportRenderer> _renderers;
    private readonly IReportOutputPresenter _outputPresenter;
}
