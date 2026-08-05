namespace JiraMetrics.Models;

/// <summary>
/// Identifies an optional dataset that can be omitted without aborting report generation.
/// </summary>
public enum OptionalReportSection
{
    /// <summary>Release report data.</summary>
    ReleaseReport,

    /// <summary>Architecture-task report data.</summary>
    ArchTasksReport,

    /// <summary>Global incident report data.</summary>
    GlobalIncidentsReport,

    /// <summary>Unresolved tasks older than 30 days.</summary>
    Unresolved30DaysTasksReport,

    /// <summary>Open-issue general statistics.</summary>
    GeneralStatistics,

    /// <summary>Roadmap report data.</summary>
    RoadmapReport,

    /// <summary>Bug ratio data.</summary>
    BugRatio,

    /// <summary>Internal incident ratio data.</summary>
    InternalIncidents,

    /// <summary>Automated test coverage data.</summary>
    TestCoverage
}
