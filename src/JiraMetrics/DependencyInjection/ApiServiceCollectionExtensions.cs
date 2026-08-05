using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

/// <summary>
/// Registers Jira API services.
/// </summary>
internal static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddJiraApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IJiraSearchExecutor, JiraSearchExecutor>()
            .AddScoped<IJiraFieldResolver, JiraFieldResolver>()
            .AddTransient<TeamTasksJqlBuilder>()
            .AddTransient<ReleaseIssuesJqlBuilder>()
            .AddTransient<ArchTasksJqlBuilder>()
            .AddTransient<GlobalIncidentsJqlBuilder>()
            .AddTransient<IJiraJqlFacade, JiraJqlFacade>()
            .AddScoped<IIssueTimelineMapper, IssueTimelineMapper>()
            .AddScoped<IJiraUserClient, JiraUserClient>()
            .AddScoped<IJiraIssueSearchClient, JiraIssueSearchClient>()
            .AddScoped<IJiraReportDataClient, JiraReportDataClient>()
            .AddScoped<IJiraIssueTimelineClient, JiraIssueTimelineClient>();
    }
}
