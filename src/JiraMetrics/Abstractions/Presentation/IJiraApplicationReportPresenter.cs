using JiraMetrics.Models;

namespace JiraMetrics.Abstractions.Presentation;

/// <summary>
/// Presents report-loading progress and successfully loaded report data.
/// </summary>
internal interface IJiraApplicationReportPresenter
{
    void ShowLoadingStarted();

    void ShowLoaded(JiraApplicationReportData reportData);
}
