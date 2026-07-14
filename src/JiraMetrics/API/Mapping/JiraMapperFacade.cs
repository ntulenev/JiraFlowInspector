using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

#pragma warning disable CS1591
namespace JiraMetrics.API.Mapping;

/// <summary>
/// Facade over the specialized Jira mappers.
/// </summary>
public sealed class JiraMapperFacade : IJiraMapperFacade
{
    public JiraMapperFacade(
        IIssueTimelineMapper issueTimelineMapper,
        IReleaseIssueMapper releaseIssueMapper,
        IGlobalIncidentMapper globalIncidentMapper,
        IRoadmapItemMapper roadmapItemMapper,
        JiraSearchIssueMapper searchIssueMapper)
    {
        ArgumentNullException.ThrowIfNull(issueTimelineMapper);
        ArgumentNullException.ThrowIfNull(releaseIssueMapper);
        ArgumentNullException.ThrowIfNull(globalIncidentMapper);
        ArgumentNullException.ThrowIfNull(roadmapItemMapper);
        ArgumentNullException.ThrowIfNull(searchIssueMapper);

        _issueTimelineMapper = issueTimelineMapper;
        _releaseIssueMapper = releaseIssueMapper;
        _globalIncidentMapper = globalIncidentMapper;
        _roadmapItemMapper = roadmapItemMapper;
        _searchIssueMapper = searchIssueMapper;
    }

    public IReadOnlyList<IssueKey> MapIssueKeys(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        JiraSearchIssueMapper.ToIssueKeys(issues ?? throw new ArgumentNullException(nameof(issues)));

    public IReadOnlyList<IssueListItem> MapIssueListItems(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        IssueListMappingContext? context = null) =>
        _searchIssueMapper.ToIssueListItems(issues ?? throw new ArgumentNullException(nameof(issues)), context);

    public IReadOnlyList<StatusIssueTypeSummary> MapStatusIssueTypeSummaries(
        IReadOnlyList<JiraIssueKeyResponse> issues) =>
        JiraSearchIssueMapper.ToStatusIssueTypeSummaries(
            issues ?? throw new ArgumentNullException(nameof(issues)));

    public IReadOnlyList<ArchTaskItem> MapArchTaskItems(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        JiraSearchIssueMapper.ToArchTaskItems(issues ?? throw new ArgumentNullException(nameof(issues)));

    public JiraSearchFields BuildReleaseRequestedFields(ReleaseIssueMappingContext context) =>
        _releaseIssueMapper.BuildRequestedFields(context);

    public IReadOnlyList<ReleaseIssueItem> MapReleaseIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context) =>
        _releaseIssueMapper.MapIssues(issues, context);

    public JiraSearchFields BuildGlobalIncidentRequestedFields(GlobalIncidentMappingContext context) =>
        _globalIncidentMapper.BuildRequestedFields(context);

    public IReadOnlyList<GlobalIncidentItem> MapGlobalIncidents(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context) =>
        _globalIncidentMapper.MapIssues(issues, context);

    public JiraSearchFields BuildRoadmapRequestedFields(RoadmapMappingContext context) =>
        _roadmapItemMapper.BuildRequestedFields(context);

    public IReadOnlyList<RoadmapItem> MapRoadmapItems(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        RoadmapMappingContext context) =>
        _roadmapItemMapper.MapIssues(issues, context);

    public IssueTimeline MapIssueTimeline(JiraIssueResponse response, IssueKey fallbackKey) =>
        _issueTimelineMapper.Map(response, fallbackKey);

    private readonly IIssueTimelineMapper _issueTimelineMapper;
    private readonly IReleaseIssueMapper _releaseIssueMapper;
    private readonly IGlobalIncidentMapper _globalIncidentMapper;
    private readonly IRoadmapItemMapper _roadmapItemMapper;
    private readonly JiraSearchIssueMapper _searchIssueMapper;
}
#pragma warning restore CS1591

