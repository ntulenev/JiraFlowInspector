using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

internal static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddJiraApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IJiraSearchExecutor, JiraSearchExecutor>()
            .AddTransient<IJiraFieldResolver, JiraFieldResolver>()
            .AddTransient<ITeamTasksJqlBuilder, TeamTasksJqlBuilder>()
            .AddTransient<IReleaseIssuesJqlBuilder, ReleaseIssuesJqlBuilder>()
            .AddTransient<IArchTasksJqlBuilder, ArchTasksJqlBuilder>()
            .AddTransient<IGlobalIncidentsJqlBuilder, GlobalIncidentsJqlBuilder>()
            .AddTransient<IJiraJqlFacade, JiraJqlFacade>()
            .AddSingleton<JiraFieldValueReader>()
            .AddTransient<IIssueTimelineMapper, IssueTimelineMapper>()
            .AddTransient<IReleaseIssueMapper, ReleaseIssueMapper>()
            .AddTransient<IGlobalIncidentMapper, GlobalIncidentMapper>()
            .AddTransient<IJiraMapperFacade, JiraMapperFacade>()
            .AddTransient<IJiraUserClient, JiraUserClient>()
            .AddTransient<IJiraIssueSearchClient, JiraIssueSearchClient>()
            .AddTransient<IJiraReportDataClient, JiraReportDataClient>()
            .AddTransient<IJiraIssueTimelineClient, JiraIssueTimelineClient>()
            .AddTransient<IJiraApiClient>(sp => new JiraApiClient(
                sp.GetRequiredService<IJiraUserClient>(),
                sp.GetRequiredService<IJiraIssueSearchClient>(),
                sp.GetRequiredService<IJiraReportDataClient>(),
                sp.GetRequiredService<IJiraIssueTimelineClient>()));
    }
}
