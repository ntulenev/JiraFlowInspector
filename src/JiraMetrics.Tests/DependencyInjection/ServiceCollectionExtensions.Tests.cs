using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

using FluentAssertions;

using JiraMetrics.API.Search;
using JiraMetrics.DependencyInjection;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;
using JiraMetrics.Presentation.Pdf;
using JiraMetrics.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

namespace JiraMetrics.Tests.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Dependency-injection extensions throw when services are null")]
    [Trait("Category", "Unit")]
    public void ExtensionMethodsWhenServicesAreNullThrowArgumentNullException()
    {
        IServiceCollection services = null!;
        var configuration = new ConfigurationBuilder().Build();

        Action addApi = () => _ = services.AddJiraApi();
        Action addApplication = () => _ = services.AddJiraApplication();
        Action addConfiguration = () => _ = services.AddJiraConfiguration(configuration);
        Action addLogic = () => _ = services.AddJiraLogic();
        Action addPdf = () => _ = services.AddJiraPdf();
        Action addPresentation = () => _ = services.AddJiraPresentation();
        Action addTransport = () => _ = services.AddJiraTransport();

        addApi.Should().Throw<ArgumentNullException>();
        addApplication.Should().Throw<ArgumentNullException>();
        addConfiguration.Should().Throw<ArgumentNullException>();
        addLogic.Should().Throw<ArgumentNullException>();
        addPdf.Should().Throw<ArgumentNullException>();
        addPresentation.Should().Throw<ArgumentNullException>();
        addTransport.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddJiraConfiguration throws when configuration is null")]
    [Trait("Category", "Unit")]
    public void AddJiraConfigurationWhenConfigurationIsNullThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        Action act = () => _ = services.AddJiraConfiguration(configuration);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AddJiraApi registers API services")]
    [Trait("Category", "Unit")]
    public void AddJiraApiWhenCalledRegistersExpectedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddJiraApi();

        returned.Should().BeSameAs(services);
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Api.IJiraSearchExecutor)
            && descriptor.ImplementationType == typeof(JiraSearchExecutor)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Api.IJiraFieldResolver)
            && descriptor.ImplementationType != null
            && descriptor.ImplementationType.Name == "JiraFieldResolver"
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Api.Mapping.IJiraMapperFacade)
            && descriptor.ImplementationType != null
            && descriptor.ImplementationType.Name == "JiraMapperFacade"
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Api.IJiraApiClient)
            && descriptor.ImplementationFactory != null
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraLogic registers logic services")]
    [Trait("Category", "Unit")]
    public void AddJiraLogicWhenCalledRegistersExpectedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddJiraLogic();

        returned.Should().BeSameAs(services);
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Logic.ITransitionBuilder)
            && descriptor.ImplementationType == typeof(TransitionBuilder)
            && descriptor.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Logic.IJiraAnalyticsService)
            && descriptor.ImplementationType == typeof(JiraAnalyticsService)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade)
            && descriptor.ImplementationType == typeof(JiraApplicationDataFacade)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade)
            && descriptor.ImplementationType == typeof(JiraApplicationAnalysisFacade)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraPdf registers PDF services")]
    [Trait("Category", "Unit")]
    public void AddJiraPdfWhenCalledRegistersExpectedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddJiraPdf();

        returned.Should().BeSameAs(services);
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Pdf.IPdfContentComposer)
            && descriptor.ImplementationType == typeof(PdfContentComposer)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Pdf.IPdfReportFileStore)
            && descriptor.ImplementationType == typeof(PdfReportFileStore)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Pdf.IPdfReportLauncher)
            && descriptor.ImplementationType == typeof(PdfReportLauncher)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Pdf.IPdfReportRenderer)
            && descriptor.ImplementationType == typeof(QuestPdfReportRenderer)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraPresentation resolves one shared Spectre presentation instance")]
    [Trait("Category", "Unit")]
    public void AddJiraPresentationWhenCalledResolvesExpectedSingletonPresenters()
    {
        var services = new ServiceCollection();
        services.AddJiraPresentation();

        using var provider = services.BuildServiceProvider();

        var presentation = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraPresentationService>();
        var status = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraStatusPresenter>();
        var progress = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraIssueLoadingProgressPresenter>();
        var sections = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraReportSectionsPresenter>();
        var analysis = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraAnalysisPresenter>();
        var diagnostics = provider.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraDiagnosticsPresenter>();

        presentation.Should().BeOfType<SpectreJiraPresentationService>();
        status.Should().BeSameAs(presentation);
        progress.Should().BeSameAs(presentation);
        sections.Should().BeSameAs(presentation);
        analysis.Should().BeSameAs(presentation);
        diagnostics.Should().BeSameAs(presentation);
    }

    [Fact(DisplayName = "AddJiraConfiguration binds Jira options and app settings")]
    [Trait("Category", "Unit")]
    public void AddJiraConfigurationWhenCalledBindsOptionsAndCreatesAppSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jira:BaseUrl"] = "https://example.atlassian.net",
                ["Jira:Email"] = "user@example.com",
                ["Jira:ApiToken"] = "secret-token",
                ["Jira:MonthLabel"] = "2026-03",
                ["Jira:TeamTasks:ProjectKey"] = "JRA",
                ["Jira:TeamTasks:DoneStatusName"] = "Done",
                ["Jira:TeamTasks:IssueTransitions:RequiredPathStages:0"] = "In Progress"
            })
            .Build();

        services.AddJiraConfiguration(configuration);

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JiraOptions>>().Value;
        var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

        options.BaseUrl.Should().Be(new Uri("https://example.atlassian.net"));
        options.TeamTasks.ProjectKey.Should().Be("JRA");
        settings.BaseUrl.Should().Be(new JiraBaseUrl("https://example.atlassian.net"));
        settings.Email.Should().Be(new JiraEmail("user@example.com"));
        settings.ApiToken.Should().Be(new JiraApiToken("secret-token"));
        settings.ProjectKey.Should().Be(new ProjectKey("JRA"));
        settings.DoneStatusName.Should().Be(new StatusName("Done"));
        settings.RequiredPathStages.Should().ContainSingle()
            .Which.Should().Be(new StageName("In Progress"));
    }

    [Fact(DisplayName = "AddJiraApplication resolves JiraApplication")]
    [Trait("Category", "Unit")]
    public void AddJiraApplicationWhenCalledResolvesApplication()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<AppSettings>>(Options.Create(CreateAppSettings()));
        services.AddSingleton<JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade>(Mock.Of<JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade>());
        services.AddSingleton<JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade>(Mock.Of<JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade>());
        services.AddSingleton<JiraMetrics.Abstractions.Presentation.IJiraStatusPresenter>(Mock.Of<JiraMetrics.Abstractions.Presentation.IJiraStatusPresenter>());
        services.AddSingleton<JiraMetrics.Abstractions.Presentation.IJiraReportSectionsPresenter>(Mock.Of<JiraMetrics.Abstractions.Presentation.IJiraReportSectionsPresenter>());
        services.AddSingleton<JiraMetrics.Abstractions.Presentation.IJiraAnalysisPresenter>(Mock.Of<JiraMetrics.Abstractions.Presentation.IJiraAnalysisPresenter>());
        services.AddSingleton<JiraMetrics.Abstractions.Presentation.IJiraDiagnosticsPresenter>(Mock.Of<JiraMetrics.Abstractions.Presentation.IJiraDiagnosticsPresenter>());
        services.AddSingleton<JiraMetrics.Abstractions.Pdf.IPdfReportRenderer>(Mock.Of<JiraMetrics.Abstractions.Pdf.IPdfReportRenderer>());
        services.AddSingleton<JiraMetrics.Abstractions.Logic.IJiraRequestTelemetryCollector>(Mock.Of<JiraMetrics.Abstractions.Logic.IJiraRequestTelemetryCollector>());
        services.AddJiraApplication();

        using var provider = services.BuildServiceProvider();

        var application = provider.GetRequiredService<JiraMetrics.Abstractions.Application.IJiraApplication>();

        application.Should().BeOfType<JiraApplication>();
    }

    [Fact(DisplayName = "AddJiraTransport resolves configured JiraTransport")]
    [Trait("Category", "Unit")]
    public void AddJiraTransportWhenCalledResolvesConfiguredTransport()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<AppSettings>>(Options.Create(CreateAppSettings()));
        services.AddJiraTransport();

        using var provider = services.BuildServiceProvider();

        var transport = provider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraTransport>();
        var httpClient = GetPrivateHttpClient((JiraTransport)transport);

        transport.Should().BeOfType<JiraTransport>();
        httpClient.BaseAddress.Should().Be(new Uri("https://example.atlassian.net/"));
        httpClient.DefaultRequestHeaders.Accept.Should().ContainSingle()
            .Which.MediaType.Should().Be("application/json");
        httpClient.DefaultRequestHeaders.Authorization.Should().Be(
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:secret-token"))));
        provider.GetRequiredService<JiraMetrics.Abstractions.Api.ISerializer>().Should().BeOfType<SimpleJsonSerializer>();
        provider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraRetryPolicy>().Should().BeOfType<JiraRetryPolicy>();
    }

    private static AppSettings CreateAppSettings() =>
        new(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("secret-token"),
            new ProjectKey("JRA"),
            new StatusName("Done"),
            rejectStatusName: null,
            requiredPathStages: [new StageName("In Progress")],
            monthLabel: new MonthLabel("2026-03"));

    private static HttpClient GetPrivateHttpClient(JiraTransport transport) =>
        (HttpClient)typeof(JiraTransport)
            .GetField("_http", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(transport)!;
}
