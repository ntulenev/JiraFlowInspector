using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Presentation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped(static sp =>
            ReportRunContext.Create(sp.GetRequiredService<TimeProvider>()));

        return services
            .AddScoped<IJiraApplicationReportLoader>(sp => new JiraApplicationReportLoader(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>()))
            .AddScoped<IJiraApplicationReportPresenter>(sp => new JiraApplicationReportPresenter(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraStatusPresenter>(),
                sp.GetRequiredService<IJiraReportSectionsPresenter>(),
                sp.GetRequiredService<IJiraDiagnosticsPresenter>()))
            .AddScoped(sp => new JiraTransitionAnalysisRunner(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<IJiraApplicationDataFacade>(),
                sp.GetRequiredService<IJiraApplicationAnalysisFacade>(),
                sp.GetRequiredService<IJiraStatusPresenter>()))
            .AddScoped(sp => new JiraReportDataFactory(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<ReportRunContext>()))
            .AddScoped<IJiraApplicationAnalysisRunner>(sp => new JiraApplicationAnalysisRunner(
                sp.GetRequiredService<IOptions<AppSettings>>().Value,
                sp.GetRequiredService<JiraTransitionAnalysisRunner>(),
                sp.GetRequiredService<IJiraPresentationService>(),
                sp.GetRequiredService<JiraReportDataFactory>(),
                sp.GetRequiredService<IJiraReportPipeline>()))
            .AddScoped<IJiraReportPipeline, JiraReportPipeline>()
            .AddScoped<IJiraApplication>(sp => new JiraApplication(
                sp.GetRequiredService<IJiraStatusPresenter>(),
                sp.GetRequiredService<IJiraRequestTelemetryCollector>(),
                sp.GetRequiredService<IJiraApplicationReportLoader>(),
                sp.GetRequiredService<IJiraApplicationReportPresenter>(),
                sp.GetRequiredService<IJiraApplicationAnalysisRunner>()));
    }
}
