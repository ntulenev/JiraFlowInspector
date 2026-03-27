using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a hot-fix marker rule.
/// </summary>
public sealed record HotFixRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HotFixRule"/> class.
    /// </summary>
    /// <param name="fieldName">Field name used by the rule.</param>
    /// <param name="values">Values that mark a release as hot-fix.</param>
    public HotFixRule(JiraFieldName fieldName, IReadOnlyList<JiraFieldValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var normalizedValues = values
            .Distinct()
            .OrderBy(static value => value.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedValues.Length == 0)
        {
            throw new ArgumentException("Hot-fix rule must contain at least one value.", nameof(values));
        }

        FieldName = fieldName;
        Values = normalizedValues;
    }

    /// <summary>
    /// Gets the field name used by the rule.
    /// </summary>
    public JiraFieldName FieldName { get; }

    /// <summary>
    /// Gets the values that mark a release as hot-fix.
    /// </summary>
    public IReadOnlyList<JiraFieldValue> Values { get; }
}
