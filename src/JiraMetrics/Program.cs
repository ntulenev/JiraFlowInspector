using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.OutputEncoding = Encoding.UTF8;
WriteStartupMessage("Starting Jira Transition Analytics...");
WriteStartupMessage("Loading configuration and building application host...");

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services
    .AddJiraConfiguration(builder.Configuration)
    .AddJiraTransport()
    .AddJiraApi()
    .AddJiraLogic()
    .AddJiraPresentation()
    .AddJiraPdf()
    .AddJiraApplication();

using var host = builder.Build();
WriteStartupMessage("Application host built. Starting services...");
await host.StartAsync().ConfigureAwait(false);
WriteStartupMessage("Services started. Launching Jira workflow...");

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var app = host.Services.GetRequiredService<IJiraApplication>();

try
{
    await app.RunAsync(lifetime.ApplicationStopping).ConfigureAwait(false);
}
catch (OperationCanceledException) when (lifetime.ApplicationStopping.IsCancellationRequested)
{
}
finally
{
    await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
}

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Startup progress messages are internal CLI status output.")]
static void WriteStartupMessage(string message)
{
    Console.WriteLine(message);
}

