using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JiraMetrics.DependencyInjection;

/// <summary>
/// Registers application workflow services.
/// </summary>
internal static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddJiraApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IJiraApplicationReportLoader>(sp => new JiraApplicationReportLoader(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>(),
                sp.GetRequiredService<IJiraStatusPresenter>(),
                sp.GetRequiredService<IJiraReportSectionsPresenter>()))
            .AddTransient<IJiraApplicationAnalysisRunner>(sp => new JiraApplicationAnalysisRunner(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>(),
                sp.GetRequiredService<IJiraApplicationAnalysisFacade>(),
                sp.GetRequiredService<IJiraStatusPresenter>(),
                sp.GetRequiredService<IJiraAnalysisPresenter>(),
                sp.GetRequiredService<IJiraDiagnosticsPresenter>(),
                sp.GetRequiredService<IJiraReportPipeline>()))
            .AddTransient<IJiraReportPipeline, JiraReportPipeline>()
            .AddTransient<IJiraApplication>(sp => new JiraApplication(
                sp.GetRequiredService<IJiraStatusPresenter>(),
                sp.GetRequiredService<IJiraRequestTelemetryCollector>(),
                sp.GetRequiredService<IJiraApplicationReportLoader>(),
                sp.GetRequiredService<IJiraApplicationAnalysisRunner>()));
    }
}
