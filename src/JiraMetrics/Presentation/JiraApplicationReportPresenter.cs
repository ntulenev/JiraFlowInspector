using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Presentation;

/// <summary>
/// Presents the report-loading portion of the application workflow.
/// </summary>
internal sealed class JiraApplicationReportPresenter : IJiraApplicationReportPresenter
{
    public JiraApplicationReportPresenter(
        AppSettings settings,
        IJiraStatusPresenter statusPresenter,
        IJiraReportSectionsPresenter reportSectionsPresenter,
        IJiraDiagnosticsPresenter diagnosticsPresenter)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(statusPresenter);
        ArgumentNullException.ThrowIfNull(reportSectionsPresenter);
        ArgumentNullException.ThrowIfNull(diagnosticsPresenter);

        _settings = settings;
        _statusPresenter = statusPresenter;
        _reportSectionsPresenter = reportSectionsPresenter;
        _diagnosticsPresenter = diagnosticsPresenter;
    }

    public void ShowLoadingStarted()
    {
        _statusPresenter.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _statusPresenter.ShowSpacer();

        if (_settings.ReleaseReport is not null)
        {
            _reportSectionsPresenter.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportSectionsPresenter.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReportLoadingStarted();
        }

        _reportSectionsPresenter.ShowAllTasksRatioLoadingStarted();

        if (_settings.BugIssueNames.Count > 0)
        {
            _reportSectionsPresenter.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
        }

        if (_settings.TestCoverage is { Enabled: true } testCoverageSettings)
        {
            _reportSectionsPresenter.ShowTestCoverageLoadingStarted(testCoverageSettings);
        }
    }

    public void ShowLoaded(JiraApplicationReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        if (reportData.OptionalSectionFailures.Count > 0)
        {
            _diagnosticsPresenter.ShowOptionalSectionFailures(reportData.OptionalSectionFailures);
            _statusPresenter.ShowSpacer();
        }

        ShowOptionalReports(reportData);

        _reportSectionsPresenter.ShowAllTasksRatioLoadingCompleted(reportData.AllTasksRatio);
        _reportSectionsPresenter.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            reportData.AllTasksRatio);
        _statusPresenter.ShowSpacer();

        if (reportData.BugRatio is { } bugRatio)
        {
            _reportSectionsPresenter.ShowBugRatioLoadingCompleted(bugRatio);
            _reportSectionsPresenter.ShowBugRatio(
                _settings.BugIssueNames,
                _settings.CustomFieldName,
                _settings.CustomFieldValue,
                bugRatio);
            _statusPresenter.ShowSpacer();
        }

        if (_settings.TestCoverage is { Enabled: true } testCoverageSettings
            && reportData.IsOptionalSectionAvailable(OptionalReportSection.TestCoverage))
        {
            _reportSectionsPresenter.ShowTestCoverage(testCoverageSettings, reportData.TestCoverage);
            _statusPresenter.ShowSpacer();
        }
    }

    private void ShowOptionalReports(JiraApplicationReportData reportData)
    {
        var reportContext = reportData.ReportContext;
        if (_settings.ReleaseReport is { } releaseReportSettings
            && reportData.IsOptionalSectionAvailable(OptionalReportSection.ReleaseReport))
        {
            _statusPresenter.ShowSpacer();
            _reportSectionsPresenter.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _statusPresenter.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings
            && reportData.IsOptionalSectionAvailable(OptionalReportSection.ArchTasksReport))
        {
            _reportSectionsPresenter.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _statusPresenter.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings
            && reportData.IsOptionalSectionAvailable(OptionalReportSection.GlobalIncidentsReport))
        {
            _reportSectionsPresenter.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _statusPresenter.ShowSpacer();
        }
    }

    private readonly AppSettings _settings;
    private readonly IJiraStatusPresenter _statusPresenter;
    private readonly IJiraReportSectionsPresenter _reportSectionsPresenter;
    private readonly IJiraDiagnosticsPresenter _diagnosticsPresenter;
}
