using System.Collections.Frozen;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Logic;

/// <summary>
/// Default transition builder implementation.
/// </summary>
public sealed class TransitionBuilder : ITransitionBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionBuilder"/> class.
    /// </summary>
    /// <param name="options">Application settings options.</param>
    public TransitionBuilder(IOptions<AppSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;
        _excludeWeekend = settings.ExcludeWeekend;
        _excludedDays = new HashSet<DateOnly>(settings.ExcludedDays).ToFrozenSet();
    }

    /// <inheritdoc />
    public IReadOnlyList<TransitionEvent> BuildTransitions(
        IReadOnlyList<(DateTimeOffset At, StatusName From, StatusName To)> rawTransitions,
        DateTimeOffset created)
    {
        ArgumentNullException.ThrowIfNull(rawTransitions);

        if (rawTransitions.Count == 0)
        {
            return [];
        }

        var ordered = rawTransitions
            .OrderBy(static item => item.At)
            .ToList();

        var transitions = new List<TransitionEvent>(ordered.Count);
        var previousAt = created;

        foreach (var (At, From, To) in ordered)
        {
            var at = At;
            if (at < created)
            {
                at = created;
            }

            if (at < previousAt)
            {
                at = previousAt;
            }

            var sincePrevious = CalculateWorkingDuration(previousAt, at);
            if (sincePrevious < TimeSpan.Zero)
            {
                sincePrevious = TimeSpan.Zero;
            }

            transitions.Add(new TransitionEvent(From, To, at, sincePrevious));
            previousAt = at;
        }

        return transitions;
    }

    private TimeSpan CalculateWorkingDuration(
        DateTimeOffset start,
        DateTimeOffset end)
    {
        if (end <= start)
        {
            return TimeSpan.Zero;
        }

        if (!_excludeWeekend && _excludedDays.Count == 0)
        {
            return end - start;
        }

        var total = TimeSpan.Zero;
        var cursor = start;

        while (cursor < end)
        {
            var nextDay = new DateTimeOffset(cursor.Date.AddDays(1), cursor.Offset);
            var segmentEnd = end < nextDay ? end : nextDay;

            var isWeekend = cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isExcluded = _excludedDays.Contains(DateOnly.FromDateTime(cursor.Date));

            if ((!_excludeWeekend || !isWeekend) && !isExcluded)
            {
                total += segmentEnd - cursor;
            }

            cursor = segmentEnd;
        }

        return total < TimeSpan.Zero ? TimeSpan.Zero : total;
    }

    private readonly bool _excludeWeekend;
    private readonly FrozenSet<DateOnly> _excludedDays;
}
