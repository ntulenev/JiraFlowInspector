using JiraMetrics.Presentation.Html;

using Microsoft.Extensions.DependencyInjection;

namespace JiraMetrics.DependencyInjection;

/// <summary>
/// Registers HTML report services.
/// </summary>
internal static class HtmlServiceCollectionExtensions
{
    public static IServiceCollection AddJiraHtml(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddTransient<IHtmlContentComposer, HtmlContentComposer>()
            .AddTransient<IHtmlReportFileStore, HtmlReportFileStore>()
            .AddTransient<IHtmlReportLauncher, HtmlReportLauncher>()
            .AddTransient<IHtmlReportRenderer, HtmlReportRenderer>();
    }
}
