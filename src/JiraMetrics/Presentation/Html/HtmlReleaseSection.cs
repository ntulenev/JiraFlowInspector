using System.Globalization;
using System.Text;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders release and component-release HTML sections.
/// </summary>
internal sealed class HtmlReleaseSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var presentationData = ReleasePresentationData.Create(reportData.Source.ReleaseIssues);
        var html = new StringBuilder();
        _ = html.Append(BuildReleaseTable(reportData, presentationData));
        _ = html.Append(BuildComponentsReleaseTable(reportData, presentationData));
        return html.ToString();
    }

    private static string BuildReleaseTable(
        JiraReportData reportData,
        ReleasePresentationData presentationData)
    {
        if (reportData.Settings.ReleaseReport is null)
        {
            return string.Empty;
        }

        var rows = presentationData.Releases
            .Select((release, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildTextCell(HtmlPresentationHelpers.FormatDate(release.ReleaseDate), release.ReleaseDate.ToDateTime(TimeOnly.MinValue).Ticks),
                BuildLinkCell(release.Key.Value, HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, release.Key)),
                BuildTextCell(release.Status.Value),
                BuildTextCell(release.Tasks == 0 ? "-" : release.Tasks.ToString(CultureInfo.InvariantCulture), release.Tasks),
                BuildTextCell(string.Join(", ", release.EnvironmentNames)),
                BuildTextCell(string.IsNullOrWhiteSpace(release.RollbackType) ? "-" : release.RollbackType),
                BuildTextCell(release.Title.Value)
            ], release.IsHotFix ? "warning-row" : null))
            .ToList();

        return BuildTableSection(
            "releases",
            "Release Report",
            "No releases found for selected period.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Release Date", "number", "Release Date"),
                new TableColumn("Jira ID", "text", "Jira ID", "issue-column"),
                new TableColumn("Status", "text", "Status"),
                new TableColumn("Tasks", "number", "Tasks"),
                new TableColumn("Environments", "text", "Environments"),
                new TableColumn("Rollback", "text", "Rollback"),
                new TableColumn("Title", "text", "Title", "summary-column")
            ],
            rows,
            defaultSortColumn: 1);
    }

    private static string BuildComponentsReleaseTable(
        JiraReportData reportData,
        ReleasePresentationData presentationData)
    {
        if (reportData.Settings.ReleaseReport is null
            || string.IsNullOrWhiteSpace(reportData.Settings.ReleaseReport.ComponentsFieldName))
        {
            return string.Empty;
        }

        var rows = presentationData.Components
            .Select((item, index) => new TableRow(
            [
                BuildTextCell((index + 1).ToString(CultureInfo.InvariantCulture), index + 1),
                BuildTextCell(item.ComponentName),
                BuildTextCell(item.ReleaseCount.Value.ToString(CultureInfo.InvariantCulture), item.ReleaseCount.Value)
            ]))
            .ToList();

        return BuildTableSection(
            "components-release-table",
            "Components Release Table",
            "No components data.",
            [
                new TableColumn("#", "number", "#", "narrow"),
                new TableColumn("Component name", "text", "Component"),
                new TableColumn("Release counts", "number", "Release counts")
            ],
            rows,
            defaultSortColumn: 2,
            defaultSortDirection: "desc",
            compact: true);
    }
}
