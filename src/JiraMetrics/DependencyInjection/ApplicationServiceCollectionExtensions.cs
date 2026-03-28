using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JiraMetrics.DependencyInjection;

internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddJiraApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IJiraApplicationReportLoader>(sp => new JiraApplicationReportLoader(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>(),
                sp.GetRequiredService<IJiraApplicationReportingFacade>()))
            .AddTransient<IJiraApplicationAnalysisRunner>(sp => new JiraApplicationAnalysisRunner(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>(),
                sp.GetRequiredService<IJiraApplicationAnalysisFacade>(),
                sp.GetRequiredService<IJiraApplicationReportingFacade>()))
            .AddTransient<IJiraApplicationReportingFacade, JiraApplicationReportingFacade>()
            .AddTransient<IJiraApplication>(sp => new JiraApplication(
                sp.GetRequiredService<IJiraApplicationReportingFacade>(),
                sp.GetRequiredService<IJiraRequestTelemetryCollector>(),
                sp.GetRequiredService<IJiraApplicationReportLoader>(),
                sp.GetRequiredService<IJiraApplicationAnalysisRunner>()));
    }
}
