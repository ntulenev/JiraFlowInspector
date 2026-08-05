using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

using FluentAssertions;

using JiraMetrics.API.Search;
using JiraMetrics.DependencyInjection;
using JiraMetrics.Logic;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Html;
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
        Action addHtml = () => _ = services.AddJiraHtml();
        Action addPdf = () => _ = services.AddJiraPdf();
        Action addPresentation = () => _ = services.AddJiraPresentation();
        Action addTransport = () => _ = services.AddJiraTransport();

        addApi.Should().Throw<ArgumentNullException>();
        addApplication.Should().Throw<ArgumentNullException>();
        addConfiguration.Should().Throw<ArgumentNullException>();
        addLogic.Should().Throw<ArgumentNullException>();
        addHtml.Should().Throw<ArgumentNullException>();
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
            && descriptor.Lifetime == ServiceLifetime.Scoped).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraLogic registers logic services")]
    [Trait("Category", "Unit")]
    public void AddJiraLogicWhenCalledRegistersExpectedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddJiraLogic();

        returned.Should().BeSameAs(services);
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(TransitionBuilder)
            && descriptor.ImplementationType == typeof(TransitionBuilder)
            && descriptor.Lifetime == ServiceLifetime.Singleton).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade)
            && descriptor.ImplementationType == typeof(JiraApplicationDataFacade)
            && descriptor.Lifetime == ServiceLifetime.Scoped).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade)
            && descriptor.ImplementationType == typeof(JiraApplicationAnalysisFacade)
            && descriptor.Lifetime == ServiceLifetime.Scoped).Should().BeTrue();
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
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IReportRenderer)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraHtml registers HTML services")]
    [Trait("Category", "Unit")]
    public void AddJiraHtmlWhenCalledRegistersExpectedServices()
    {
        var services = new ServiceCollection();

        var returned = services.AddJiraHtml();

        returned.Should().BeSameAs(services);
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Html.IHtmlContentComposer)
            && descriptor.ImplementationType == typeof(HtmlContentComposer)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Html.IHtmlReportFileStore)
            && descriptor.ImplementationType == typeof(HtmlReportFileStore)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Html.IHtmlReportLauncher)
            && descriptor.ImplementationType == typeof(HtmlReportLauncher)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
        services.Any(static descriptor =>
            descriptor.ServiceType == typeof(JiraMetrics.Abstractions.Application.IReportRenderer)
            && descriptor.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }

    [Fact(DisplayName = "AddJiraPresentation resolves report-run scoped presenters")]
    [Trait("Category", "Unit")]
    public void AddJiraPresentationWhenCalledResolvesExpectedSingletonPresenters()
    {
        var services = new ServiceCollection();
        services.AddJiraPresentation();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var presentation = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraPresentationService>();
        var status = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraStatusPresenter>();
        var progress = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraIssueLoadingProgressPresenter>();
        var sections = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraReportSectionsPresenter>();
        var analysis = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraAnalysisPresenter>();
        var diagnostics = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IJiraDiagnosticsPresenter>();
        var reportOutput = scopedServices.GetRequiredService<JiraMetrics.Abstractions.Presentation.IReportOutputPresenter>();

        presentation.Should().BeOfType<SpectreJiraPresentationService>();
        status.Should().BeSameAs(presentation);
        progress.Should().BeOfType<SpectreIssueLoadingProgressPresenter>();
        sections.Should().BeOfType<SpectreReportSectionsPresenter>();
        analysis.Should().BeSameAs(sections);
        diagnostics.Should().BeSameAs(sections);
        reportOutput.Should().BeSameAs(presentation);
    }

    [Fact(DisplayName = "Report-run state is shared within a scope and isolated between scopes")]
    [Trait("Category", "Unit")]
    public void ReportRunServicesWhenResolvedAcrossScopesUseScopeBoundaries()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<AppSettings>>(Options.Create(CreateAppSettings()));
        services.AddSingleton<JiraMetrics.Abstractions.Api.IJiraTransport>(
            new Mock<JiraMetrics.Abstractions.Api.IJiraTransport>(MockBehavior.Strict).Object);
        services.AddJiraApi();
        services.AddJiraPresentation();
        services.AddJiraApplication();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstRunContext = firstScope.ServiceProvider.GetRequiredService<JiraMetrics.Models.ReportRunContext>();
        var repeatedRunContext = firstScope.ServiceProvider.GetRequiredService<JiraMetrics.Models.ReportRunContext>();
        var secondRunContext = secondScope.ServiceProvider.GetRequiredService<JiraMetrics.Models.ReportRunContext>();
        var firstFieldResolver = firstScope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraFieldResolver>();
        var repeatedFieldResolver = firstScope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraFieldResolver>();
        var secondFieldResolver = secondScope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraFieldResolver>();

        repeatedRunContext.Should().BeSameAs(firstRunContext);
        secondRunContext.Should().NotBeSameAs(firstRunContext);
        repeatedFieldResolver.Should().BeSameAs(firstFieldResolver);
        secondFieldResolver.Should().NotBeSameAs(firstFieldResolver);
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
        services.AddSingleton<JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade>(new Mock<JiraMetrics.Abstractions.Application.IJiraApplicationDataFacade>(MockBehavior.Strict).Object);
        services.AddSingleton<JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade>(new Mock<JiraMetrics.Abstractions.Application.IJiraApplicationAnalysisFacade>(MockBehavior.Strict).Object);
        services.AddSingleton<JiraMetrics.Abstractions.Application.IReportRenderer>(new Mock<JiraMetrics.Abstractions.Application.IReportRenderer>(MockBehavior.Strict).Object);
        services.AddSingleton<JiraMetrics.Abstractions.Logic.IJiraRequestTelemetryCollector>(new Mock<JiraMetrics.Abstractions.Logic.IJiraRequestTelemetryCollector>(MockBehavior.Strict).Object);
        services.AddJiraPresentation();
        services.AddJiraApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var application = scope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Application.IJiraApplication>();

        application.Should().BeOfType<JiraApplication>();
    }

    [Fact(DisplayName = "AddJiraTransport resolves configured JiraTransport")]
    [Trait("Category", "Unit")]
    public void AddJiraTransportWhenCalledResolvesConfiguredTransport()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<AppSettings>>(Options.Create(CreateAppSettings()));
        services.AddJiraTransport();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var transport = scope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraTransport>();
        var httpClient = GetPrivateHttpClient((JiraTransport)transport);

        transport.Should().BeOfType<JiraTransport>();
        httpClient.BaseAddress.Should().Be(new Uri("https://example.atlassian.net/"));
        httpClient.DefaultRequestHeaders.Accept.Should().ContainSingle()
            .Which.MediaType.Should().Be("application/json");
        httpClient.DefaultRequestHeaders.Authorization.Should().Be(
            new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:secret-token"))));
        scope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.ISerializer>().Should().BeOfType<SimpleJsonSerializer>();
        scope.ServiceProvider.GetRequiredService<JiraMetrics.Abstractions.Api.IJiraRetryPolicy>().Should().BeOfType<JiraRetryPolicy>();
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
