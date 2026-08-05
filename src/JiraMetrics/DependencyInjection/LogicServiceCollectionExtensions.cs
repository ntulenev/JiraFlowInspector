using JiraMetrics.Logic;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

/// <summary>
/// Registers report-analysis and application logic services.
/// </summary>
internal static class LogicServiceCollectionExtensions
{
    public static IServiceCollection AddJiraLogic(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<TransitionBuilder>()
            .AddTransient<IJiraLogicService, JiraLogicService>()
            .AddScoped<IssueSearchSnapshotLoader>()
            .AddScoped<TestCoverageLoader>()
            .AddScoped<JiraReportContextLoader>()
            .AddScoped<JiraIssueTimelineLoader>()
            .AddScoped<IJiraApplicationDataFacade, JiraApplicationDataFacade>()
            .AddScoped<IJiraApplicationAnalysisFacade, JiraApplicationAnalysisFacade>();
    }
}
