using FluentAssertions;

using JiraMetrics.Abstractions;
using JiraMetrics.Logic;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Tests.Logic;

public sealed class JiraApplicationTests
{
    [Fact(DisplayName = "Constructor throws when settings are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSettingsAreNullThrowsArgumentNullException()
    {
        // Arrange
        IOptions<AppSettings> settings = null!;
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when API client is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenApiClientIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        IJiraApiClient apiClient = null!;
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when settings value is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenSettingsValueIsNullThrowsArgumentException()
    {
        // Arrange
        var settings = Options.Create<AppSettings>(null!);
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var presentation = new FakePresentationService();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Constructor throws when logic service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenLogicServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        var apiClient = new FakeApiClient();
        IJiraLogicService logic = null!;
        var presentation = new FakePresentationService();

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor throws when presentation service is null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenPresentationServiceIsNullThrowsArgumentNullException()
    {
        // Arrange
        var settings = Options.Create(CreateSettings());
        var apiClient = new FakeApiClient();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        IJiraPresentationService presentation = null!;

        // Act
        Action act = () => _ = new JiraApplication(settings, apiClient, logic, presentation);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RunAsync shows no issues matched filter when search returns empty list")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenSearchReturnsEmptyListShowsNoIssuesMatchedFilter()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = []
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(Options.Create(CreateSettings()), apiClient, logic, presentation);

        // Act
        await app.RunAsync();

        // Assert
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.DoneIssuesTableShown.Should().BeFalse();
    }

    [Fact(DisplayName = "RunAsync shows failures when issue loading fails")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueLoadingFailsShowsFailures()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            FailIssueKeys = [new("AAA-2")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(Options.Create(CreateSettings()), apiClient, logic, presentation);

        // Act
        await app.RunAsync();

        // Assert
        presentation.DoneIssuesTableShown.Should().BeTrue();
        presentation.FailuresShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows authentication failure and rethrows when auth fails")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenAuthenticationFailsShowsFailureAndRethrows()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            ThrowOnAuth = true
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(Options.Create(CreateSettings()), apiClient, logic, presentation);

        // Act
        Func<Task> act = () => app.RunAsync();

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        presentation.AuthenticationFailedShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows no issues matched filter when issue type filter excludes all loaded issues")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueTypeFilterExcludesAllIssuesShowsNoIssuesMatchedFilter()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task"))
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(
            Options.Create(CreateSettings([new IssueTypeName("Bug"), new IssueTypeName("Story")])),
            apiClient,
            logic,
            presentation);

        // Act
        await app.RunAsync();

        // Assert
        presentation.NoIssuesMatchedFilterShown.Should().BeTrue();
        presentation.DoneIssuesTableShown.Should().BeFalse();
    }

    private static AppSettings CreateSettings(IReadOnlyList<IssueTypeName>? issueTypes = null)
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StageName("Code Review"),
            new MonthLabel("2026-02"),
            null,
            issueTypes,
            excludeWeekend: false);
    }

    private static IssueTimeline CreateIssue(IssueKey key, IssueTypeName? issueType = null)
    {
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow, TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };

        return new IssueTimeline(
            key,
            issueType ?? new IssueTypeName("Story"),
            new IssueSummary($"Summary {key.Value}"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions));
    }

    private sealed class FakeApiClient : IJiraApiClient
    {
        public JiraAuthUser CurrentUser { get; set; } = new(new UserDisplayName("unknown"), null, null);

        public IReadOnlyList<IssueKey> IssueKeys { get; set; } = [];

        public HashSet<IssueKey> FailIssueKeys { get; set; } = [];

        public IssueTimeline? IssueToReturn { get; set; }

        public bool ThrowOnAuth { get; set; }

        public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnAuth)
            {
                throw new InvalidOperationException("Auth failed.");
            }

            return Task.FromResult(CurrentUser);
        }

        public Task<IReadOnlyList<IssueKey>> GetIssueKeysMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            CreatedAfterDate? createdAfter,
            CancellationToken cancellationToken) => Task.FromResult(IssueKeys);

        public Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken)
        {
            if (FailIssueKeys.Contains(issueKey))
            {
                throw new InvalidOperationException("Failed to load issue.");
            }

            if (IssueToReturn is null)
            {
                throw new InvalidOperationException("No issue configured for fake transport.");
            }

            return Task.FromResult(new IssueTimeline(
                issueKey,
                IssueToReturn.IssueType,
                IssueToReturn.Summary,
                IssueToReturn.Created,
                IssueToReturn.EndTime,
                IssueToReturn.Transitions,
                IssueToReturn.PathKey,
                IssueToReturn.PathLabel));
        }
    }

    private sealed class FakePresentationService : IJiraPresentationService
    {
        public bool AuthenticationFailedShown { get; private set; }

        public bool NoIssuesMatchedFilterShown { get; private set; }

        public bool DoneIssuesTableShown { get; private set; }

        public bool FailuresShown { get; private set; }

        public void ShowAuthenticationStarted()
        {
        }

        public void ShowAuthenticationSucceeded(JiraAuthUser user)
        {
        }

        public void ShowAuthenticationFailed(ErrorMessage errorMessage) => AuthenticationFailedShown = true;

        public void ShowIssueSearchFailed(ErrorMessage errorMessage)
        {
        }

        public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
        {
        }

        public void ShowNoIssuesMatchedFilter() => NoIssuesMatchedFilterShown = true;

        public void ShowIssueLoaded(IssueKey issueKey)
        {
        }

        public void ShowIssueFailed(IssueKey issueKey)
        {
        }

        public void ShowSpacer()
        {
        }

        public void ShowNoIssuesLoaded()
        {
        }

        public void ShowNoIssuesMatchedRequiredStage()
        {
        }

        public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName) => DoneIssuesTableShown = true;

        public void ShowPathGroupsSummary(PathGroupsSummary summary)
        {
        }

        public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
        {
        }

        public void ShowFailures(IReadOnlyList<LoadFailure> failures)
        {
            if (failures.Count > 0)
            {
                FailuresShown = true;
            }
        }
    }
}

