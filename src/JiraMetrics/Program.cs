using System.Globalization;
using System.Net.Http.Headers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using JiraMetrics.Abstractions;
using JiraMetrics.API;
using JiraMetrics.API.FieldResolution;
using JiraMetrics.API.Jql;
using JiraMetrics.API.Mapping;
using JiraMetrics.API.Search;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Pdf;
using JiraMetrics.Presentation;
using JiraMetrics.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.OutputEncoding = Encoding.UTF8;
WriteStartupMessage("Starting Jira Transition Analytics...");
WriteStartupMessage("Loading configuration and building application host...");

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
    var teamTasks = source.TeamTasks ?? throw new InvalidOperationException("TeamTasks section is required.");
    var monthLabel = string.IsNullOrWhiteSpace(source.MonthLabel)
        ? MonthLabel.CurrentUtc()
        : new MonthLabel(source.MonthLabel);
    var createdAfter = string.IsNullOrWhiteSpace(source.CreatedAfter)
        ? (CreatedAfterDate?)null
        : new CreatedAfterDate(source.CreatedAfter);
    StageName[] requiredPathStages = teamTasks.IssueTransitions?.RequiredPathStages is null
        ? []
        : [.. teamTasks.IssueTransitions.RequiredPathStages
            .Where(static stage => !string.IsNullOrWhiteSpace(stage))
            .Select(static stage => new StageName(stage))
            .DistinctBy(static stage => stage.Value, StringComparer.OrdinalIgnoreCase)];
    if (requiredPathStages.Length == 0)
    {
        throw new InvalidOperationException("At least one TeamTasks:IssueTransitions:RequiredPathStages entry must be configured.");
    }

    IssueTypeName[] issueTypes = teamTasks.IssueTransitions?.IssueTypes is null
        ? []
        : [.. teamTasks.IssueTransitions.IssueTypes
            .Where(static issueType => !string.IsNullOrWhiteSpace(issueType))
            .Select(static issueType => new IssueTypeName(issueType))
            .DistinctBy(static issueType => issueType.Value, StringComparer.OrdinalIgnoreCase)];
    IssueTypeName[] bugIssueNames = teamTasks.BugRatio?.BugIssueNames is null
        ? []
        : [.. teamTasks.BugRatio.BugIssueNames
            .Where(static issueType => !string.IsNullOrWhiteSpace(issueType))
            .Select(static issueType => new IssueTypeName(issueType))
            .DistinctBy(static issueType => issueType.Value, StringComparer.OrdinalIgnoreCase)];
    var customFieldName = string.IsNullOrWhiteSpace(teamTasks.CustomFieldName) ? null : teamTasks.CustomFieldName.Trim();
    var customFieldValue = string.IsNullOrWhiteSpace(teamTasks.CustomFieldValue) ? null : teamTasks.CustomFieldValue.Trim();
    var rejectStatusName = string.IsNullOrWhiteSpace(teamTasks.RejectStatusName)
        ? (StatusName?)null
        : new StatusName(teamTasks.RejectStatusName);
    var releaseReport = ResolveReleaseReport(source.ReleaseReport);
    var globalIncidentsReport = ResolveGlobalIncidentsReport(source.GlobalIncidents);
    var pdfReport = ResolvePdfReport(source.Pdf);
    DateOnly[] excludedDays = teamTasks.IssueTransitions?.ExcludedDays is null
        ? []
        : [.. teamTasks.IssueTransitions.ExcludedDays
            .Where(static day => !string.IsNullOrWhiteSpace(day))
            .Select(static day => ParseExcludedDay(day.Trim()))
            .Distinct()];

    var settings = new AppSettings(
        new JiraBaseUrl(source.BaseUrl.ToString()),
        new JiraEmail(source.Email),
        new JiraApiToken(source.ApiToken),
        new ProjectKey(teamTasks.ProjectKey),
        new StatusName(teamTasks.DoneStatusName),
        rejectStatusName,
        requiredPathStages,
        monthLabel,
        createdAfter,
        issueTypes,
        customFieldName,
        customFieldValue,
        source.ShowTimeCalculationsInHoursOnly,
        teamTasks.IssueTransitions?.ExcludeWeekend ?? false,
        excludedDays,
        bugIssueNames,
        teamTasks.ShowGeneralStatistics,
        releaseReport,
        globalIncidentsReport,
        pdfReport,
        source.PullRequestFieldName);

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
builder.Services.AddSingleton<ITransitionBuilder, TransitionBuilder>();
builder.Services.AddTransient<IJiraSearchExecutor, JiraSearchExecutor>();
builder.Services.AddTransient<IJiraFieldResolver, JiraFieldResolver>();
builder.Services.AddTransient<ITeamTasksJqlBuilder, TeamTasksJqlBuilder>();
builder.Services.AddTransient<IReleaseIssuesJqlBuilder, ReleaseIssuesJqlBuilder>();
builder.Services.AddTransient<IGlobalIncidentsJqlBuilder, GlobalIncidentsJqlBuilder>();
builder.Services.AddTransient<IJiraJqlFacade, JiraJqlFacade>();
builder.Services.AddSingleton<JiraFieldValueReader>();
builder.Services.AddTransient<IIssueTimelineMapper, IssueTimelineMapper>();
builder.Services.AddTransient<IReleaseIssueMapper, ReleaseIssueMapper>();
builder.Services.AddTransient<IGlobalIncidentMapper, GlobalIncidentMapper>();
builder.Services.AddTransient<IJiraMapperFacade, JiraMapperFacade>();
builder.Services.AddTransient<IJiraApiClient, JiraApiClient>();
builder.Services.AddTransient<IJiraAnalyticsService, JiraAnalyticsService>();
builder.Services.AddTransient<IJiraLogicService, JiraLogicService>();
builder.Services.AddTransient<JiraReportContextLoader>();
builder.Services.AddTransient<JiraIssueRatioLoader>();
builder.Services.AddTransient<JiraIssueTimelineLoader>();
builder.Services.AddTransient<IJiraApplicationDataFacade, JiraApplicationDataFacade>();
builder.Services.AddTransient<IJiraApplicationAnalysisFacade, JiraApplicationAnalysisFacade>();
builder.Services.AddTransient<IJiraPresentationService, SpectreJiraPresentationService>();
builder.Services.AddTransient<IPdfContentComposer, PdfContentComposer>();
builder.Services.AddTransient<IPdfReportFileStore, PdfReportFileStore>();
builder.Services.AddTransient<IPdfReportLauncher, PdfReportLauncher>();
builder.Services.AddTransient<IPdfReportRenderer, QuestPdfReportRenderer>();
builder.Services.AddTransient<IJiraApplication, JiraApplication>();

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

static DateOnly ParseExcludedDay(string value)
{
    if (DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
    {
        return day;
    }

    if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
    {
        return day;
    }

    throw new FormatException($"Invalid excluded day '{value}'. Expected dd.MM.yyyy or yyyy-MM-dd.");
}

static ReleaseReportSettings? ResolveReleaseReport(ReleaseReportOptions? source)
{
    if (source is null)
    {
        return null;
    }

    var hasAnyValue =
        !string.IsNullOrWhiteSpace(source.ReleaseProjectKey)
        || !string.IsNullOrWhiteSpace(source.ProjectLabel)
        || !string.IsNullOrWhiteSpace(source.ReleaseDateFieldName)
        || !string.IsNullOrWhiteSpace(source.ComponentsFieldName)
        || source.HotFixRules is { Count: > 0 }
        || !string.IsNullOrWhiteSpace(source.RollbackFieldName)
        || !string.IsNullOrWhiteSpace(source.EnvironmentFieldName)
        || !string.IsNullOrWhiteSpace(source.EnvironmentFieldValue);

    if (!hasAnyValue)
    {
        return null;
    }

    if (string.IsNullOrWhiteSpace(source.ReleaseProjectKey)
        || string.IsNullOrWhiteSpace(source.ProjectLabel)
        || string.IsNullOrWhiteSpace(source.ReleaseDateFieldName))
    {
        throw new InvalidOperationException(
            "ReleaseReport requires ReleaseProjectKey, ProjectLabel, and ReleaseDateFieldName when configured.");
    }

    return new ReleaseReportSettings(
        new ProjectKey(source.ReleaseProjectKey),
        source.ProjectLabel,
        source.ReleaseDateFieldName,
        source.ComponentsFieldName,
        source.HotFixRules?.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)(pair.Value ?? []),
            StringComparer.OrdinalIgnoreCase),
        source.RollbackFieldName,
        source.EnvironmentFieldName,
        source.EnvironmentFieldValue);
}

static GlobalIncidentsReportSettings? ResolveGlobalIncidentsReport(GlobalIncidentsReportOptions? source)
{
    if (source is null)
    {
        return null;
    }

    return new GlobalIncidentsReportSettings(
        source.Namespace,
        source.JqlFilter,
        source.SearchPhrase,
        source.IncidentStartFieldName,
        source.IncidentStartFallbackFieldName,
        source.IncidentRecoveryFieldName,
        source.IncidentRecoveryFallbackFieldName,
        source.ImpactFieldName,
        source.UrgencyFieldName,
        source.AdditionalFieldNames);
}

static PdfReportSettings ResolvePdfReport(PdfOptions? source)
{
    if (source is null)
    {
        return new PdfReportSettings();
    }

    return new PdfReportSettings(source.Enabled, source.OutputPath, source.OpenAfterGeneration);
}

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Startup progress messages are internal CLI status output.")]
static void WriteStartupMessage(string message)
{
    Console.WriteLine(message);
}
