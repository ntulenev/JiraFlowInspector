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

    [Fact(DisplayName = "RunAsync shows bug ratio section when bug issue names are configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenBugIssueNamesAreConfiguredShowsBugRatio()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Bug")),
            CreatedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-10"), new IssueSummary("Open bug"))],
            MovedToDoneThisMonthIssues = [new IssueListItem(new IssueKey("AAA-1"), new IssueSummary("Done bug"))],
            RejectedThisMonthIssues = [new IssueListItem(new IssueKey("AAA-11"), new IssueSummary("Rejected bug"))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")])),
            apiClient,
            logic,
            presentation);

        // Act
        await app.RunAsync();

        // Assert
        apiClient.CreatedThisMonthCountRequested.Should().BeFalse();
        apiClient.MovedToDoneThisMonthCountRequested.Should().BeFalse();
        apiClient.CreatedThisMonthIssuesRequested.Should().BeTrue();
        apiClient.MovedToDoneThisMonthIssuesRequested.Should().BeTrue();
        apiClient.RejectedThisMonthIssuesRequested.Should().BeTrue();
        presentation.BugRatioLoadingStartedShown.Should().BeTrue();
        presentation.BugRatioLoadingCompletedShown.Should().BeTrue();
        presentation.BugRatioShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows release report section when release report is configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseReportIsConfiguredShowsReleaseReport()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Task")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))]
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Task")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "Processing",
                    "Change completion date"))),
            apiClient,
            logic,
            presentation);

        // Act
        await app.RunAsync();

        // Assert
        apiClient.ReleaseIssuesRequested.Should().BeTrue();
        presentation.ReleaseReportShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync shows release report before bug ratio and header when both are enabled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenReleaseAndBugRatioAreConfiguredShowsReleaseFirst()
    {
        // Arrange
        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = CreateIssue(new IssueKey("AAA-1"), new IssueTypeName("Bug")),
            ReleaseIssues = [new ReleaseIssueItem(new IssueKey("RLS-1"), new IssueSummary("Release item"), new DateOnly(2026, 2, 14))],
            CreatedThisMonthIssues = [],
            MovedToDoneThisMonthIssues = [],
            RejectedThisMonthIssues = []
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(
            Options.Create(CreateSettings(
                issueTypes: [new IssueTypeName("Bug")],
                bugIssueNames: [new IssueTypeName("Bug")],
                releaseReport: new ReleaseReportSettings(
                    new ProjectKey("RLS"),
                    "ADF",
                    "Change completion date"))),
            apiClient,
            logic,
            presentation);

        // Act
        await app.RunAsync();

        // Assert
        var releaseIndex = presentation.Calls.IndexOf("ReleaseReport");
        var periodContextIndex = presentation.Calls.IndexOf("ReportPeriodContext");
        var bugRatioLoadingStartIndex = presentation.Calls.IndexOf("BugRatioLoadingStarted");
        var bugRatioIndex = presentation.Calls.IndexOf("BugRatio");
        var headerIndex = presentation.Calls.IndexOf("ReportHeader");
        periodContextIndex.Should().BeGreaterThanOrEqualTo(0);
        releaseIndex.Should().BeGreaterThanOrEqualTo(0);
        bugRatioLoadingStartIndex.Should().BeGreaterThanOrEqualTo(0);
        bugRatioIndex.Should().BeGreaterThanOrEqualTo(0);
        headerIndex.Should().BeGreaterThanOrEqualTo(0);
        periodContextIndex.Should().BeLessThan(releaseIndex);
        releaseIndex.Should().BeLessThan(bugRatioLoadingStartIndex);
        releaseIndex.Should().BeLessThan(bugRatioIndex);
        releaseIndex.Should().BeLessThan(headerIndex);
    }

    [Fact(DisplayName = "RunAsync shows rejected issues table when reject status is configured")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenRejectStatusIsConfiguredShowsRejectedIssuesTable()
    {
        // Arrange
        var rejectTransitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Reject"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };
        var rejectedIssue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("Rejected task"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            rejectTransitions,
            PathKey.FromTransitions(rejectTransitions),
            PathLabel.FromTransitions(rejectTransitions),
            hasPullRequest: true);

        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1")],
            IssueToReturn = rejectedIssue
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])), apiClient, logic, presentation);

        // Act
        await app.RunAsync();

        // Assert
        presentation.RejectedIssuesTableShown.Should().BeTrue();
    }

    [Fact(DisplayName = "RunAsync transition analysis uses only issues with code")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncWhenIssueHasNoCodeExcludesItFromTransitionAnalysis()
    {
        // Arrange
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Code Review"), DateTimeOffset.UtcNow.AddHours(-2), TimeSpan.FromHours(1)),
            new(new StatusName("Code Review"), new StatusName("Done"), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromHours(1))
        };

        var issueWithCode = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Task"),
            new IssueSummary("With code"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: true);

        var issueWithoutCode = new IssueTimeline(
            new IssueKey("AAA-2"),
            new IssueTypeName("Task"),
            new IssueSummary("Without code"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            hasPullRequest: false);

        var apiClient = new FakeApiClient
        {
            CurrentUser = new JiraAuthUser(new UserDisplayName("Nikita"), "user@example.com", "123"),
            IssueKeys = [new IssueKey("AAA-1"), new IssueKey("AAA-2")],
            IssuesByKey =
            {
                ["AAA-1"] = issueWithCode,
                ["AAA-2"] = issueWithoutCode
            }
        };

        var presentation = new FakePresentationService();
        var logic = new JiraLogicService(new JiraAnalyticsService());
        var app = new JiraApplication(
            Options.Create(CreateSettings(issueTypes: [new IssueTypeName("Task")])),
            apiClient,
            logic,
            presentation);

        // Act
        await app.RunAsync();

        // Assert
        presentation.DoneIssuesTableShown.Should().BeTrue();
        presentation.DoneIssues.Should().HaveCount(2);
        presentation.DoneIssues.Select(static issue => issue.Key.Value).Should().BeEquivalentTo(["AAA-1", "AAA-2"]);
        presentation.PathGroupsSummary.Should().NotBeNull();
        presentation.PathGroupsSummary!.SuccessfulCount.Value.Should().Be(2);
        presentation.PathGroupsSummary.MatchedStageCount.Value.Should().Be(1);
    }

    private static AppSettings CreateSettings(
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        IReadOnlyList<IssueTypeName>? bugIssueNames = null,
        ReleaseReportSettings? releaseReport = null)
    {
        return new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            null,
            issueTypes,
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: false,
            bugIssueNames: bugIssueNames,
            releaseReport: releaseReport);
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
            PathLabel.FromTransitions(transitions),
            hasPullRequest: true);
    }

    private sealed class FakeApiClient : IJiraApiClient
    {
        public JiraAuthUser CurrentUser { get; set; } = new(new UserDisplayName("unknown"), null, null);

        public IReadOnlyList<IssueKey> IssueKeys { get; set; } = [];

        public HashSet<IssueKey> FailIssueKeys { get; set; } = [];

        public IssueTimeline? IssueToReturn { get; set; }

        public Dictionary<string, IssueTimeline> IssuesByKey { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool ThrowOnAuth { get; set; }

        public ItemCount CreatedThisMonthCount { get; set; } = new(0);

        public ItemCount MovedToDoneThisMonthCount { get; set; } = new(0);

        public IReadOnlyList<IssueListItem> CreatedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> MovedToDoneThisMonthIssues { get; set; } = [];

        public IReadOnlyList<IssueListItem> RejectedThisMonthIssues { get; set; } = [];

        public IReadOnlyList<ReleaseIssueItem> ReleaseIssues { get; set; } = [];

        public bool CreatedThisMonthCountRequested { get; private set; }

        public bool MovedToDoneThisMonthCountRequested { get; private set; }

        public bool CreatedThisMonthIssuesRequested { get; private set; }

        public bool MovedToDoneThisMonthIssuesRequested { get; private set; }

        public bool RejectedThisMonthIssuesRequested { get; private set; }

        public bool ReleaseIssuesRequested { get; private set; }

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

        public Task<ItemCount> GetIssueCountCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            CreatedThisMonthCountRequested = true;
            return Task.FromResult(CreatedThisMonthCount);
        }

        public Task<ItemCount> GetIssueCountMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            MovedToDoneThisMonthCountRequested = true;
            return Task.FromResult(MovedToDoneThisMonthCount);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesCreatedThisMonthAsync(
            ProjectKey projectKey,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            CreatedThisMonthIssuesRequested = true;
            return Task.FromResult(CreatedThisMonthIssues);
        }

        public Task<IReadOnlyList<IssueListItem>> GetIssuesMovedToDoneThisMonthAsync(
            ProjectKey projectKey,
            StatusName doneStatusName,
            IReadOnlyList<IssueTypeName> issueTypes,
            CancellationToken cancellationToken)
        {
            if (string.Equals(doneStatusName.Value, "Reject", StringComparison.OrdinalIgnoreCase))
            {
                RejectedThisMonthIssuesRequested = true;
                return Task.FromResult(RejectedThisMonthIssues);
            }

            MovedToDoneThisMonthIssuesRequested = true;
            return Task.FromResult(MovedToDoneThisMonthIssues);
        }

        public Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
            ProjectKey releaseProjectKey,
            string projectLabel,
            string releaseDateFieldName,
            string? componentsFieldName,
            CancellationToken cancellationToken)
        {
            ReleaseIssuesRequested = true;
            return Task.FromResult(ReleaseIssues);
        }

        public Task<IssueTimeline> GetIssueTimelineAsync(IssueKey issueKey, CancellationToken cancellationToken)
        {
            if (FailIssueKeys.Contains(issueKey))
            {
                throw new InvalidOperationException("Failed to load issue.");
            }

            if (IssuesByKey.TryGetValue(issueKey.Value, out var configuredIssue))
            {
                return Task.FromResult(new IssueTimeline(
                    issueKey,
                    configuredIssue.IssueType,
                    configuredIssue.Summary,
                    configuredIssue.Created,
                    configuredIssue.EndTime,
                    configuredIssue.Transitions,
                    configuredIssue.PathKey,
                    configuredIssue.PathLabel,
                    configuredIssue.SubItemsCount,
                    configuredIssue.HasPullRequest));
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
                IssueToReturn.PathLabel,
                IssueToReturn.SubItemsCount,
                IssueToReturn.HasPullRequest));
        }
    }

    private sealed class FakePresentationService : IJiraPresentationService
    {
        public List<string> Calls { get; } = [];

        public bool AuthenticationFailedShown { get; private set; }

        public bool NoIssuesMatchedFilterShown { get; private set; }

        public bool DoneIssuesTableShown { get; private set; }

        public bool RejectedIssuesTableShown { get; private set; }

        public bool FailuresShown { get; private set; }

        public bool BugRatioShown { get; private set; }

        public bool BugRatioLoadingStartedShown { get; private set; }

        public bool BugRatioLoadingCompletedShown { get; private set; }

        public bool ReleaseReportShown { get; private set; }

        public IReadOnlyList<IssueTimeline> DoneIssues { get; private set; } = [];

        public IReadOnlyList<IssueTimeline> RejectedIssues { get; private set; } = [];

        public PathGroupsSummary? PathGroupsSummary { get; private set; }

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

        public void ShowReportPeriodContext(MonthLabel monthLabel, CreatedAfterDate? createdAfter) => Calls.Add("ReportPeriodContext");

        public void ShowReportHeader(AppSettings settings, ItemCount issueCount) => Calls.Add("ReportHeader");

        public void ShowNoIssuesMatchedFilter() => NoIssuesMatchedFilterShown = true;

        public void ShowIssueLoadingStarted(ItemCount totalIssues)
        {
        }

        public void ShowIssueLoaded(IssueKey issueKey)
        {
        }

        public void ShowIssueFailed(IssueKey issueKey)
        {
        }

        public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues)
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

        public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
        {
            DoneIssues = [.. issues];
            DoneIssuesTableShown = issues.Count > 0;
        }

        public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
        {
            RejectedIssues = [.. issues];
            if (issues.Count > 0)
            {
                RejectedIssuesTableShown = true;
            }
        }

        public void ShowPathGroupsSummary(PathGroupsSummary summary)
        {
            PathGroupsSummary = summary;
        }

        public void ShowReleaseReport(
            ReleaseReportSettings settings,
            MonthLabel monthLabel,
            IReadOnlyList<ReleaseIssueItem> releases)
        {
            ReleaseReportShown = true;
            Calls.Add("ReleaseReport");
        }

        public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
        {
            BugRatioLoadingStartedShown = true;
            Calls.Add("BugRatioLoadingStarted");
        }

        public void ShowBugRatioLoadingCompleted(
            ItemCount createdThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth)
        {
            BugRatioLoadingCompletedShown = true;
            Calls.Add("BugRatioLoadingCompleted");
        }

        public void ShowBugRatio(
            IReadOnlyList<IssueTypeName> bugIssueNames,
            string? customFieldName,
            string? customFieldValue,
            ItemCount createdThisMonth,
            ItemCount movedToDoneThisMonth,
            ItemCount rejectedThisMonth,
            ItemCount finishedThisMonth,
            IReadOnlyList<IssueListItem> openIssues,
            IReadOnlyList<IssueListItem> doneIssues,
            IReadOnlyList<IssueListItem> rejectedIssues)
        {
            BugRatioShown = true;
            Calls.Add("BugRatio");
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

