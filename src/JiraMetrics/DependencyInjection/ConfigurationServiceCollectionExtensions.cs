using JiraMetrics.Models.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JiraMetrics.DependencyInjection;

internal static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddJiraConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptions<JiraOptions>()
            .Bind(configuration.GetSection("Jira"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services.AddSingleton(sp =>
        {
            var source = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            return Options.Create(AppSettingsFactory.Create(source));
        });
    }
}
