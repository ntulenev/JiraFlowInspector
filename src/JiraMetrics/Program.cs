using Spectre.Console;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var jiraBaseUrl = "";
var jiraEmail = "";
var jiraApiToken = "";
var projectKey = "";
var doneStatusName = "Done";
var requiredPathStage = Environment.GetEnvironmentVariable("REQUIRED_PATH_STAGE");
if (string.IsNullOrWhiteSpace(requiredPathStage))
    requiredPathStage = "Code Review";
var monthLabel = DateTime.UtcNow.ToString("yyyy-MM");


using var http = BuildHttpClient(jiraEmail, jiraApiToken);
var (issueKeys, issueKeysError) = await FetchIssueKeysMovedToDoneThisMonthAsync(http, jiraBaseUrl, projectKey, doneStatusName);

if (issueKeysError is not null)
{
    AnsiConsole.MarkupLine($"[red]Failed to load issues:[/] {Markup.Escape(issueKeysError)}");
    return;
}

AnsiConsole.Write(
    new Rule($"[bold cyan]Jira Transition Analytics[/] - [bold]{issueKeys.Count} issue(s)[/]")
        .RuleStyle("grey")
        .LeftJustified());

AnsiConsole.MarkupLine($"[grey]Filter:[/] project = {Markup.Escape(projectKey)}, moved to {Markup.Escape(doneStatusName)} in {Markup.Escape(monthLabel)}");
AnsiConsole.MarkupLine($"[grey]Required stage in path:[/] {Markup.Escape(requiredPathStage)}");

if (issueKeys.Count == 0)
{
    AnsiConsole.MarkupLine("[yellow]No issues matched this filter.[/]");
    return;
}

var issues = new List<IssueTimeline>();
var failures = new List<(string key, string reason)>();

foreach (var issueKey in issueKeys)
{
    var (issue, error) = await FetchIssueTimelineAsync(http, jiraBaseUrl, issueKey);
    if (issue is null)
    {
        failures.Add((issueKey, error ?? "Unknown error"));
        AnsiConsole.MarkupLine($"[red]Failed[/] {Markup.Escape(issueKey)}");
        continue;
    }

    issues.Add(issue);
    AnsiConsole.MarkupLine($"[green]Loaded[/] {Markup.Escape(issueKey)}");
}

AnsiConsole.WriteLine();

if (issues.Count == 0)
{
    AnsiConsole.MarkupLine("[red]No issues were loaded successfully.[/]");
    PrintFailures(failures);
    return;
}

var filteredIssues = issues
    .Where(x => PathContainsStage(x, requiredPathStage))
    .ToList();

if (filteredIssues.Count == 0)
{
    AnsiConsole.MarkupLine("[yellow]No issues matched the required stage in path.[/]");
    PrintFailures(failures);
    return;
}

PrintDoneIssuesTable(filteredIssues, doneStatusName);
AnsiConsole.WriteLine();

var groups = BuildPathGroups(filteredIssues);

AnsiConsole.MarkupLine($"[grey]Successful:[/] {issues.Count}    [grey]Matched stage:[/] {filteredIssues.Count}    [grey]Failed:[/] {failures.Count}    [grey]Path groups:[/] {groups.Count}");
AnsiConsole.WriteLine();

for (var i = 0; i < groups.Count; i++)
{
    var group = groups[i];

    AnsiConsole.Write(
        new Rule($"[bold]Group {i + 1}[/] - [bold]{group.Issues.Count} issue(s)[/]")
            .RuleStyle("grey")
            .LeftJustified());
    AnsiConsole.MarkupLine($"[grey]Path:[/] {Markup.Escape(group.PathLabel)}");
    AnsiConsole.MarkupLine($"[grey]Issues:[/] {Markup.Escape(string.Join(", ", group.Issues.Select(x => x.Key)))}");
    AnsiConsole.WriteLine();

    if (group.P75Transitions.Count == 0)
    {
        AnsiConsole.MarkupLine("[grey]No transitions in this path.[/]");
        AnsiConsole.WriteLine();
        continue;
    }

    AnsiConsole.MarkupLine("[bold]P75 Timeline Diagram[/]");
    var segments = group.P75Transitions
        .Select(x => (label: $"{x.From} -> {x.To}", duration: x.P75Duration))
        .ToList();
    RenderDurationTimeline(segments);
    AnsiConsole.WriteLine();

    var table = new Table()
        .RoundedBorder()
        .BorderColor(Color.Grey)
        .AddColumn("[bold]From[/]")
        .AddColumn("[bold]To[/]")
        .AddColumn("[bold]P75 Time[/]")
        .AddColumn("[bold]Samples[/]");

    foreach (var transition in group.P75Transitions)
    {
        table.AddRow(
            Markup.Escape(transition.From),
            Markup.Escape(transition.To),
            FormatDuration(transition.P75Duration),
            group.Issues.Count.ToString());
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

if (failures.Count > 0)
{
    AnsiConsole.WriteLine();
    PrintFailures(failures);
}

// ---- helpers
static HttpClient BuildHttpClient(string jiraEmail, string jiraApiToken)
{
    var http = new HttpClient();
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{jiraEmail}:{jiraApiToken}"));
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    return http;
}

static async Task<(List<string> issueKeys, string? error)> FetchIssueKeysMovedToDoneThisMonthAsync(
    HttpClient http,
    string jiraBaseUrl,
    string projectKey,
    string doneStatusName)
{
    var escapedProject = EscapeJqlString(projectKey);
    var escapedDoneStatus = EscapeJqlString(doneStatusName);
    var jql = $"project = \"{escapedProject}\" AND status CHANGED TO \"{escapedDoneStatus}\" AFTER startOfMonth() ORDER BY key ASC";

    var issueKeys = new List<string>();
    const int pageSize = 100;
    string? nextPageToken = null;

    while (true)
    {
        var searchUrlBuilder = new StringBuilder(
            $"{jiraBaseUrl}/rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&fields=key&maxResults={pageSize}");
        if (!string.IsNullOrWhiteSpace(nextPageToken))
            searchUrlBuilder.Append($"&nextPageToken={Uri.EscapeDataString(nextPageToken)}");

        var searchUrl = searchUrlBuilder.ToString();

        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(searchUrl);
        }
        catch (Exception ex)
        {
            return (new List<string>(), $"HTTP error: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return (new List<string>(), $"{(int)response.StatusCode} {response.ReasonPhrase} {Truncate(body, 200)}");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("issues", out var issuesEl) || issuesEl.ValueKind != JsonValueKind.Array)
                return (new List<string>(), "Search response is missing issues array.");

            var pageKeys = new List<string>();
            foreach (var issueEl in issuesEl.EnumerateArray())
            {
                if (!issueEl.TryGetProperty("key", out var keyEl) ||
                    keyEl.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(keyEl.GetString()))
                {
                    continue;
                }

                pageKeys.Add(keyEl.GetString()!);
            }

            issueKeys.AddRange(pageKeys);

            var isLast = root.TryGetProperty("isLast", out var isLastEl) &&
                         isLastEl.ValueKind == JsonValueKind.True;

            nextPageToken = root.TryGetProperty("nextPageToken", out var nextPageTokenEl) &&
                            nextPageTokenEl.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(nextPageTokenEl.GetString())
                ? nextPageTokenEl.GetString()
                : null;

            if (pageKeys.Count == 0 || isLast || string.IsNullOrWhiteSpace(nextPageToken))
                break;
        }
        catch (Exception ex)
        {
            return (new List<string>(), $"Parse error: {ex.Message}");
        }
    }

    var distinctKeys = issueKeys
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();

    return (distinctKeys, null);
}

static async Task<(IssueTimeline? issue, string? error)> FetchIssueTimelineAsync(HttpClient http, string jiraBaseUrl, string issueKey)
{
    var issueUrl = $"{jiraBaseUrl}/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}?expand=changelog";

    HttpResponseMessage response;
    try
    {
        response = await http.GetAsync(issueUrl);
    }
    catch (Exception ex)
    {
        return (null, $"HTTP error: {ex.Message}");
    }

    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return (null, $"{(int)response.StatusCode} {response.ReasonPhrase} {Truncate(body, 200)}");
    }

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("fields", out var fields))
            return (null, "Response missing fields.");

        var summary = fields.TryGetProperty("summary", out var summaryEl)
            ? summaryEl.GetString() ?? string.Empty
            : string.Empty;

        if (!fields.TryGetProperty("created", out var createdEl) ||
            createdEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(createdEl.GetString()))
        {
            return (null, "Issue created date is missing.");
        }

        var created = DateTimeOffset.Parse(createdEl.GetString()!);
        var transitions = ParseTransitions(root, created);

        var endTime = DateTimeOffset.UtcNow;
        if (fields.TryGetProperty("resolutiondate", out var resolutionEl) &&
            resolutionEl.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(resolutionEl.GetString()))
        {
            endTime = DateTimeOffset.Parse(resolutionEl.GetString()!);
        }

        if (endTime < created)
            endTime = created;

        return (new IssueTimeline(
            issueKey,
            summary,
            created,
            endTime,
            transitions,
            BuildPathKey(transitions),
            BuildPathLabel(transitions)), null);
    }
    catch (Exception ex)
    {
        return (null, $"Parse error: {ex.Message}");
    }
}

static List<TransitionEvent> ParseTransitions(JsonElement issueRoot, DateTimeOffset created)
{
    var rawTransitions = new List<(DateTimeOffset at, string from, string to)>();

    if (issueRoot.TryGetProperty("changelog", out var changelog) &&
        changelog.TryGetProperty("histories", out var histories) &&
        histories.ValueKind == JsonValueKind.Array)
    {
        foreach (var history in histories.EnumerateArray())
        {
            if (!history.TryGetProperty("created", out var historyCreatedEl) ||
                historyCreatedEl.ValueKind != JsonValueKind.String ||
                !DateTimeOffset.TryParse(historyCreatedEl.GetString(), out var at))
            {
                continue;
            }

            if (!history.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("field", out var fieldEl) ||
                    !string.Equals(fieldEl.GetString(), "status", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var from = NormalizeStatus(item.TryGetProperty("fromString", out var fromEl) ? fromEl.GetString() : null);
                var to = NormalizeStatus(item.TryGetProperty("toString", out var toEl) ? toEl.GetString() : null);
                rawTransitions.Add((at, from, to));
            }
        }
    }

    rawTransitions = rawTransitions.OrderBy(x => x.at).ToList();

    var transitions = new List<TransitionEvent>(rawTransitions.Count);
    var previousAt = created;

    foreach (var raw in rawTransitions)
    {
        var at = raw.at;
        if (at < created) at = created;
        if (at < previousAt) at = previousAt;

        var sincePrevious = at - previousAt;
        if (sincePrevious < TimeSpan.Zero) sincePrevious = TimeSpan.Zero;

        transitions.Add(new TransitionEvent(raw.from, raw.to, at, sincePrevious));
        previousAt = at;
    }

    return transitions;
}

static List<PathGroup> BuildPathGroups(IReadOnlyList<IssueTimeline> issues)
{
    return issues
        .GroupBy(x => x.PathKey, StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            var groupedIssues = group.ToList();
            var template = groupedIssues[0].Transitions;
            var p75Transitions = new List<PercentileTransition>(template.Count);

            for (var i = 0; i < template.Count; i++)
            {
                var samples = groupedIssues
                    .Select(x => x.Transitions[i].SincePrevious)
                    .ToList();

                var p75 = CalculatePercentile(samples, 0.75);
                p75Transitions.Add(new PercentileTransition(template[i].From, template[i].To, p75));
            }

            var totalP75 = p75Transitions.Aggregate(TimeSpan.Zero, (acc, x) => acc + x.P75Duration);

            return new PathGroup(
                groupedIssues[0].PathLabel,
                groupedIssues,
                p75Transitions,
                totalP75);
        })
        .OrderByDescending(x => x.Issues.Count)
        .ThenBy(x => x.PathLabel, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static void RenderDurationTimeline(IReadOnlyList<(string label, TimeSpan duration)> segments)
{
    if (segments.Count == 0)
    {
        AnsiConsole.MarkupLine("[grey]No transitions to render.[/]");
        return;
    }

    var normalized = segments
        .Select(x => (x.label, duration: x.duration < TimeSpan.Zero ? TimeSpan.Zero : x.duration))
        .ToList();

    var width = Math.Clamp(AnsiConsole.Profile.Width - 16, 30, 100);
    var total = normalized.Aggregate(TimeSpan.Zero, (acc, x) => acc + x.duration);
    var totalSeconds = Math.Max(0.0, total.TotalSeconds);

    var bar = new StringBuilder();
    var cursor = 0;
    var cumulativeSeconds = 0.0;

    for (var i = 0; i < normalized.Count; i++)
    {
        var segment = normalized[i];
        var startPos = totalSeconds <= 0
            ? 0
            : (int)Math.Round((cumulativeSeconds / totalSeconds) * (width - 1));
        var endCumulativeSeconds = cumulativeSeconds + segment.duration.TotalSeconds;
        var endPos = totalSeconds <= 0
            ? width - 1
            : (int)Math.Round((endCumulativeSeconds / totalSeconds) * (width - 1));

        if (startPos < cursor) startPos = cursor;
        if (endPos < startPos) endPos = startPos;
        if (i == normalized.Count - 1) endPos = width - 1;

        if (startPos > cursor)
            bar.Append(new string(' ', startPos - cursor));

        var segmentWidth = Math.Max(1, endPos - startPos + 1);
        var maxWidth = width - startPos;
        if (maxWidth <= 0) break;
        if (segmentWidth > maxWidth) segmentWidth = maxWidth;

        bar.Append($"[{GetTimelineColor(i)}]{new string('█', segmentWidth)}[/]");
        cursor = startPos + segmentWidth;
        cumulativeSeconds = endCumulativeSeconds;
    }

    if (cursor < width)
        bar.Append(new string(' ', width - cursor));

    AnsiConsole.MarkupLine(bar.ToString());

    var left = "0";
    var right = $"P75 total: {FormatDuration(total)}";
    var spacing = Math.Max(1, width - left.Length - right.Length);
    AnsiConsole.MarkupLine($"[grey]{Markup.Escape(left)}{new string(' ', spacing)}{Markup.Escape(right)}[/]");

    var legend = string.Join(
        "  ",
        normalized.Select((x, i) => $"[{GetTimelineColor(i)}]■[/] {Markup.Escape(x.label)}"));
    AnsiConsole.MarkupLine(legend);
}

static void PrintFailures(IReadOnlyList<(string key, string reason)> failures)
{
    if (failures.Count == 0)
        return;

    AnsiConsole.MarkupLine("[bold red]Failed issues[/]");

    var table = new Table()
        .RoundedBorder()
        .BorderColor(Color.Grey)
        .AddColumn("[bold]Issue[/]")
        .AddColumn("[bold]Reason[/]");

    foreach (var failure in failures)
    {
        table.AddRow(Markup.Escape(failure.key), Markup.Escape(failure.reason));
    }

    AnsiConsole.Write(table);
}

static void PrintDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, string doneStatusName)
{
    if (issues.Count == 0)
        return;

    AnsiConsole.MarkupLine("[bold]Issues moved to Done this month[/]");

    var table = new Table()
        .RoundedBorder()
        .BorderColor(Color.Grey)
        .AddColumn("[bold]Issue[/]")
        .AddColumn("[bold]Summary[/]")
        .AddColumn("[bold]Last Done At[/]");

    foreach (var issue in issues.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
    {
        var lastDoneAt = issue.Transitions
            .Where(x => string.Equals(x.To, doneStatusName, StringComparison.OrdinalIgnoreCase))
            .Select(x => (DateTimeOffset?)x.At)
            .OrderByDescending(x => x)
            .FirstOrDefault();

        var lastDoneAtText = lastDoneAt.HasValue
            ? lastDoneAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "-";

        table.AddRow(
            Markup.Escape(issue.Key),
            Markup.Escape(Truncate(issue.Summary, 120)),
            Markup.Escape(lastDoneAtText));
    }

    AnsiConsole.Write(table);
}

static string BuildPathKey(IReadOnlyList<TransitionEvent> transitions)
{
    if (transitions.Count == 0)
        return "__NO_TRANSITIONS__";

    return string.Join("||", transitions.Select(x => $"{x.From.ToUpperInvariant()}->{x.To.ToUpperInvariant()}"));
}

static string BuildPathLabel(IReadOnlyList<TransitionEvent> transitions)
{
    if (transitions.Count == 0)
        return "No transitions";

    return string.Join(" | ", transitions.Select(x => $"{x.From} -> {x.To}"));
}

static TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> values, double percentile)
{
    if (values.Count == 0)
        return TimeSpan.Zero;

    percentile = Math.Clamp(percentile, 0.0, 1.0);

    var sorted = values
        .Select(x => Math.Max(0.0, x.TotalSeconds))
        .OrderBy(x => x)
        .ToList();

    if (sorted.Count == 1)
        return TimeSpan.FromSeconds(sorted[0]);

    var rank = (sorted.Count - 1) * percentile;
    var lowerIndex = (int)Math.Floor(rank);
    var upperIndex = (int)Math.Ceiling(rank);
    var fraction = rank - lowerIndex;
    var interpolated = sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * fraction;

    return TimeSpan.FromSeconds(interpolated);
}

static string NormalizeStatus(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "Unknown";

    return value.Trim();
}

static bool PathContainsStage(IssueTimeline issue, string requiredStage)
{
    if (string.IsNullOrWhiteSpace(requiredStage))
        return true;

    return issue.Transitions.Any(t =>
        t.From.Contains(requiredStage, StringComparison.OrdinalIgnoreCase) ||
        t.To.Contains(requiredStage, StringComparison.OrdinalIgnoreCase));
}

static string EscapeJqlString(string value)
{
    return value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}

static string Truncate(string value, int maxLength)
{
    if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        return value;

    return value[..(maxLength - 3)] + "...";
}

static string GetTimelineColor(int index) => index switch
{
    0 => "deepskyblue1",
    1 => "dodgerblue1",
    2 => "cyan1",
    3 => "springgreen1",
    4 => "yellow1",
    5 => "orange1",
    _ => "grey82"
};

static string FormatDuration(TimeSpan ts)
{
    if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;

    var days = (int)ts.TotalDays;
    var hours = ts.Hours;
    var minutes = ts.Minutes;
    var seconds = ts.Seconds;

    var parts = new List<string>();
    if (days > 0) parts.Add($"{days}d");
    if (hours > 0) parts.Add($"{hours}h");
    if (minutes > 0) parts.Add($"{minutes}m");
    if (parts.Count == 0) parts.Add($"{seconds}s");

    return string.Join(" ", parts);
}

record TransitionEvent(string From, string To, DateTimeOffset At, TimeSpan SincePrevious);

record IssueTimeline(
    string Key,
    string Summary,
    DateTimeOffset Created,
    DateTimeOffset EndTime,
    IReadOnlyList<TransitionEvent> Transitions,
    string PathKey,
    string PathLabel);

record PercentileTransition(string From, string To, TimeSpan P75Duration);

record PathGroup(
    string PathLabel,
    IReadOnlyList<IssueTimeline> Issues,
    IReadOnlyList<PercentileTransition> P75Transitions,
    TimeSpan TotalP75);
