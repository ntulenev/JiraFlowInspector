using JiraMetrics.Presentation.Pdf;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

internal static class PdfServiceCollectionExtensions
{
    public static IServiceCollection AddJiraPdf(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IPdfContentComposer, PdfContentComposer>()
            .AddTransient<IPdfReportFileStore, PdfReportFileStore>()
            .AddTransient<IPdfReportLauncher, PdfReportLauncher>()
            .AddTransient<IPdfReportRenderer, QuestPdfReportRenderer>();
    }
}
