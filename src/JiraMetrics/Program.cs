using System.Net.Http.Headers;
using System.Text;

using JiraMetrics.Abstractions;
using JiraMetrics.API;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;
using JiraMetrics.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.OutputEncoding = Encoding.UTF8;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services
    .AddOptions<JiraOptions>()
    .Bind(builder.Configuration.GetSection("Jira"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var source = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
    var monthLabel = string.IsNullOrWhiteSpace(source.MonthLabel)
        ? MonthLabel.CurrentUtc()
        : new MonthLabel(source.MonthLabel);
    var createdAfter = string.IsNullOrWhiteSpace(source.CreatedAfter)
        ? (CreatedAfterDate?)null
        : new CreatedAfterDate(source.CreatedAfter);
    IssueTypeName[] issueTypes = source.IssueTypes is null
        ? []
        : [.. source.IssueTypes
            .Where(static issueType => !string.IsNullOrWhiteSpace(issueType))
            .Select(static issueType => new IssueTypeName(issueType))
            .DistinctBy(static issueType => issueType.Value, StringComparer.OrdinalIgnoreCase)];

    var settings = new AppSettings(
        new JiraBaseUrl(source.BaseUrl.ToString()),
        new JiraEmail(source.Email),
        new JiraApiToken(source.ApiToken),
        new ProjectKey(source.ProjectKey),
        new StatusName(source.DoneStatusName),
        new StageName(source.RequiredPathStage),
        monthLabel,
        createdAfter,
        issueTypes,
        source.ExcludeWeekend);

    return Options.Create(settings);
});

builder.Services.AddHttpClient<IJiraTransport, JiraTransport>((sp, http) =>
{
    var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    http.BaseAddress = new Uri(settings.BaseUrl.ToString().TrimEnd('/') + "/");

    var raw = $"{settings.Email.Value}:{settings.ApiToken.Value}";
    var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", b64);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<ISerializer, SimpleJsonSerializer>();
builder.Services.AddSingleton<IJiraRetryPolicy, JiraRetryPolicy>();
builder.Services.AddTransient<IJiraApiClient, JiraApiClient>();
builder.Services.AddTransient<IJiraAnalyticsService, JiraAnalyticsService>();
builder.Services.AddTransient<IJiraLogicService, JiraLogicService>();
builder.Services.AddTransient<IJiraPresentationService, SpectreJiraPresentationService>();
builder.Services.AddTransient<IJiraApplication, JiraApplication>();

using var host = builder.Build();

var app = host.Services.GetRequiredService<IJiraApplication>();
await app.RunAsync(CancellationToken.None).ConfigureAwait(false);
