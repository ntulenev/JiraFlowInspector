using System.Net.Http.Headers;
using System.Text;

using JiraMetrics.Models.Configuration;
using JiraMetrics.Transport;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JiraMetrics.DependencyInjection;

internal static class TransportServiceCollectionExtensions
{
    public static IServiceCollection AddJiraTransport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddHttpClient<IJiraTransport, JiraTransport>((sp, http) =>
            {
                var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                http.BaseAddress = new Uri(settings.BaseUrl.ToString().TrimEnd('/') + "/");

                var raw = $"{settings.Email.Value}:{settings.ApiToken.Value}";
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", b64);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .Services
            .AddSingleton<ISerializer, SimpleJsonSerializer>()
            .AddSingleton<IJiraRequestTelemetryCollector, JiraRequestTelemetryCollector>()
            .AddSingleton<IJiraRetryPolicy, JiraRetryPolicy>();

        return services;
    }
}
