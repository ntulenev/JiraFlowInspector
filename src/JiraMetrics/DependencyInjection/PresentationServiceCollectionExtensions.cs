using JiraMetrics.Presentation;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

internal static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddJiraPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<SpectreJiraPresentationService>()
            .AddSingleton<IJiraPresentationService>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddSingleton<IJiraStatusPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddSingleton<IJiraIssueLoadingProgressPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddSingleton<IJiraReportSectionsPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddSingleton<IJiraAnalysisPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddSingleton<IJiraDiagnosticsPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>());
    }
}
