using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders issue-ratio HTML sections.
/// </summary>
internal sealed class HtmlRatiosSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var presentationData = RatioSectionPresentationData.Create(reportData);
        return BuildRatiosSection(presentationData);
    }

    private static string BuildRatiosSection(RatioSectionPresentationData presentationData)
    {
        var rows = new List<TableRow>();
        AddRatioRows(rows, "All tasks", presentationData.AllTasks);
        AddRatioRows(rows, "Bugs", presentationData.Bugs);
        if (presentationData.Bugs is { } bugRatio)
        {
            rows.Add(BuildMetricRow("Bugs: Reproduced on prod", bugRatio.ReproducedOnProdCount.Value));
        }

        return BuildTableSection(
            "ratios",
            "Task Ratios",
            "No ratio data available.",
            MetricColumns,
            rows,
            defaultSortColumn: 0,
            compact: true);
    }

    private static void AddRatioRows(
        List<TableRow> rows,
        string scope,
        IssueRatioPresentationData? snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        rows.Add(BuildMetricRow($"{scope}: Created", snapshot.CreatedCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Open", snapshot.OpenCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Done", snapshot.DoneCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Rejected", snapshot.RejectedCount.Value));
        rows.Add(BuildMetricRow($"{scope}: Finished", snapshot.FinishedCount.Value));
        rows.Add(new TableRow(
        [
            BuildTextCell($"{scope}: Finished / Created"),
            BuildTextCell(snapshot.FinishedToCreatedRatioText)
        ]));
    }
}
