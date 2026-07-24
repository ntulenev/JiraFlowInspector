using JiraMetrics.Models;

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

    public void RenderReport(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        foreach (var renderer in _renderers)
        {
            var outputs = renderer.RenderReport(reportData);
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
