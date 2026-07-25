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
            .AddTransient<IssueSearchSnapshotLoader>()
            .AddTransient<TestCoverageLoader>()
            .AddTransient<JiraReportContextLoader>()
            .AddTransient<JiraIssueTimelineLoader>()
            .AddTransient<IJiraApplicationDataFacade, JiraApplicationDataFacade>()
            .AddTransient<IJiraApplicationAnalysisFacade, JiraApplicationAnalysisFacade>();
    }
}
