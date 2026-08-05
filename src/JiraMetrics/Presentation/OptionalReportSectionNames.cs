using JiraMetrics.Models;

namespace JiraMetrics.Presentation;

/// <summary>
/// Provides user-facing names for optional report sections.
/// </summary>
internal static class OptionalReportSectionNames
{
    public static string GetDisplayName(OptionalReportSection section) =>
        section switch
        {
            OptionalReportSection.ReleaseReport => "Release report",
            OptionalReportSection.ArchTasksReport => "Architecture tasks",
            OptionalReportSection.GlobalIncidentsReport => "Global incidents",
            OptionalReportSection.Unresolved30DaysTasksReport => "Unresolved tasks older than 30 days",
            OptionalReportSection.GeneralStatistics => "General statistics",
            OptionalReportSection.RoadmapReport => "Roadmap",
            OptionalReportSection.BugRatio => "Bug ratio",
            OptionalReportSection.InternalIncidents => "Internal incidents",
            OptionalReportSection.TestCoverage => "Test coverage",
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown optional report section.")
        };
}
