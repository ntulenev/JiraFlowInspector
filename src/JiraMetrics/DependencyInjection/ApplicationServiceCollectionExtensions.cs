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

        return services.AddTransient<IJiraApplication>(sp => new JiraApplication(
            sp.GetRequiredService<IOptions<AppSettings>>(),
            sp.GetRequiredService<IJiraApplicationDataFacade>(),
            sp.GetRequiredService<IJiraApplicationAnalysisFacade>(),
            sp.GetRequiredService<IJiraStatusPresenter>(),
            sp.GetRequiredService<IJiraReportSectionsPresenter>(),
            sp.GetRequiredService<IJiraAnalysisPresenter>(),
            sp.GetRequiredService<IJiraDiagnosticsPresenter>(),
            sp.GetRequiredService<IPdfReportRenderer>(),
            sp.GetRequiredService<IJiraRequestTelemetryCollector>()));
    }
}
