using JiraMetrics.Presentation;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

/// <summary>
/// Registers console presentation services.
/// </summary>
internal static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddJiraPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddScoped<SpectreJiraPresentationService>()
            .AddScoped<IJiraPresentationService>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddScoped<IJiraStatusPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>())
            .AddScoped<IJiraIssueLoadingProgressPresenter>(sp =>
                sp.GetRequiredService<SpectreJiraPresentationService>().ProgressPresenter)
            .AddScoped<IJiraReportSectionsPresenter>(sp =>
                sp.GetRequiredService<SpectreJiraPresentationService>().ReportSectionsPresenter)
            .AddScoped<IJiraAnalysisPresenter>(sp =>
                sp.GetRequiredService<SpectreJiraPresentationService>().ReportSectionsPresenter)
            .AddScoped<IJiraDiagnosticsPresenter>(sp =>
                sp.GetRequiredService<SpectreJiraPresentationService>().ReportSectionsPresenter)
            .AddScoped<IReportOutputPresenter>(sp => sp.GetRequiredService<SpectreJiraPresentationService>());
    }
}
