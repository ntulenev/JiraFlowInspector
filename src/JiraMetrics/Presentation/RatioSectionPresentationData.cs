using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared ratio data shared by console, HTML, and PDF presentations.
/// </summary>
internal sealed class RatioSectionPresentationData
{
    private RatioSectionPresentationData(
        IssueRatioPresentationData? allTasks,
        IssueRatioPresentationData? bugs,
        IssueRatioPresentationData? internalIncidents,
        TestCoveragePresentationData? testCoverage,
        string bugIssueTypesLabel,
        string internalIncidentIssueTypesLabel)
    {
        AllTasks = allTasks;
        Bugs = bugs;
        InternalIncidents = internalIncidents;
        TestCoverage = testCoverage;
        BugIssueTypesLabel = bugIssueTypesLabel;
        InternalIncidentIssueTypesLabel = internalIncidentIssueTypesLabel;
    }

    public IssueRatioPresentationData? AllTasks { get; }

    public IssueRatioPresentationData? Bugs { get; }

    public IssueRatioPresentationData? InternalIncidents { get; }

    public TestCoveragePresentationData? TestCoverage { get; }

    public string BugIssueTypesLabel { get; }

    public string InternalIncidentIssueTypesLabel { get; }

    public static RatioSectionPresentationData Create(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var settings = reportData.Settings;
        var ratios = reportData.Ratios;
        return new RatioSectionPresentationData(
            CreateOptionalRatio(ratios.AllTasks),
            CreateOptionalRatio(ratios.Bugs),
            settings.InternalIncidentIssueNames.Count == 0
                ? null
                : CreateOptionalRatio(ratios.InternalIncidents),
            settings.TestCoverage is { Enabled: true } testCoverageSettings
                ? TestCoveragePresentationData.Create(testCoverageSettings, ratios.TestCoverage)
                : null,
            JoinIssueTypes(settings.BugIssueNames, emptyValue: "-"),
            JoinIssueTypes(settings.InternalIncidentIssueNames, emptyValue: string.Empty));
    }

    private static IssueRatioPresentationData? CreateOptionalRatio(IssueRatioSnapshot? snapshot) =>
        snapshot is null ? null : IssueRatioPresentationData.Create(snapshot);

    private static string JoinIssueTypes(
        IReadOnlyList<IssueTypeName> issueTypes,
        string emptyValue) =>
        issueTypes.Count == 0
            ? emptyValue
            : string.Join(", ", issueTypes.Select(static issueType => issueType.Value));
}

/// <summary>
/// Prepared counters and issue lists for one ratio scope.
/// </summary>
internal sealed class IssueRatioPresentationData
{
    private IssueRatioPresentationData(
        IssueRatioSnapshot snapshot,
        IReadOnlyList<IssueListItem> openIssues,
        IReadOnlyList<IssueListItem> doneIssues,
        IReadOnlyList<IssueListItem> rejectedIssues)
    {
        CreatedCount = snapshot.CreatedThisMonth;
        OpenCount = snapshot.OpenThisMonth;
        DoneCount = snapshot.MovedToDoneThisMonth;
        RejectedCount = snapshot.RejectedThisMonth;
        FinishedCount = snapshot.FinishedThisMonth;
        OpenIssues = openIssues;
        DoneIssues = doneIssues;
        RejectedIssues = rejectedIssues;
        ReproducedOnProdCount = new ItemCount(snapshot.ReporducedOnProdIssues.Count);
        OpenReproducedOnProdCount = CountReproducedOnProd(openIssues);
        DoneReproducedOnProdCount = CountReproducedOnProd(doneIssues);
        RejectedReproducedOnProdCount = CountReproducedOnProd(rejectedIssues);
        FinishedReproducedOnProdCount = new ItemCount(
            doneIssues
                .Concat(rejectedIssues)
                .Where(static issue => issue.ReporducedOnProd)
                .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
                .Count());
        FinishedToCreatedRatioText = PresentationFormatting.BuildFinishedToCreatedRatioText(
            CreatedCount,
            FinishedCount);
    }

    public ItemCount CreatedCount { get; }

    public ItemCount OpenCount { get; }

    public ItemCount DoneCount { get; }

    public ItemCount RejectedCount { get; }

    public ItemCount FinishedCount { get; }

    public ItemCount ReproducedOnProdCount { get; }

    public ItemCount OpenReproducedOnProdCount { get; }

    public ItemCount DoneReproducedOnProdCount { get; }

    public ItemCount RejectedReproducedOnProdCount { get; }

    public ItemCount FinishedReproducedOnProdCount { get; }

    public string FinishedToCreatedRatioText { get; }

    public IReadOnlyList<IssueListItem> OpenIssues { get; }

    public IReadOnlyList<IssueListItem> DoneIssues { get; }

    public IReadOnlyList<IssueListItem> RejectedIssues { get; }

    public bool HasIssueDetails =>
        OpenIssues.Count > 0 || DoneIssues.Count > 0 || RejectedIssues.Count > 0;

    public static IssueRatioPresentationData Create(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new IssueRatioPresentationData(
            snapshot,
            OrderIssues(snapshot.OpenIssues),
            OrderIssues(snapshot.DoneIssues),
            OrderIssues(snapshot.RejectedIssues));
    }

    private static IReadOnlyList<IssueListItem> OrderIssues(IReadOnlyList<IssueListItem> issues) =>
        [.. issues.OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    private static ItemCount CountReproducedOnProd(IEnumerable<IssueListItem> issues) =>
        new(issues.Count(static issue => issue.ReporducedOnProd));
}

/// <summary>
/// Prepared automated test coverage data shared by all presentations.
/// </summary>
internal sealed class TestCoveragePresentationData
{
    private TestCoveragePresentationData(
        string issueTypesLabel,
        string testProjectLabel,
        string linkLabel,
        ItemCount totalIssues,
        ItemCount coveredIssueCount,
        double? coveragePercentage)
    {
        IssueTypesLabel = issueTypesLabel;
        TestProjectLabel = testProjectLabel;
        LinkLabel = linkLabel;
        TotalIssues = totalIssues;
        CoveredIssueCount = coveredIssueCount;
        CoveragePercentage = coveragePercentage;
        CoverageText = PresentationFormatting.FormatPercentage(coveragePercentage);
    }

    public string IssueTypesLabel { get; }

    public string TestProjectLabel { get; }

    public string LinkLabel { get; }

    public ItemCount TotalIssues { get; }

    public ItemCount CoveredIssueCount { get; }

    public double? CoveragePercentage { get; }

    public string CoverageText { get; }

    public static TestCoveragePresentationData Create(
        TestCoverageSettings settings,
        TestCoverageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(snapshot);

        return new TestCoveragePresentationData(
            string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value)),
            settings.TestProjectKey.Value,
            settings.LinkName,
            snapshot.TotalIssues,
            snapshot.CoveredIssueCount,
            snapshot.CoveragePercentage);
    }
}
