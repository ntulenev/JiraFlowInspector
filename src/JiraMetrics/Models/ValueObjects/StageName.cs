namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a required path stage name used for filtering.
/// </summary>
public readonly record struct StageName
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageName"/> struct.
    /// </summary>
    /// <param name="value">Stage name text.</param>
    public StageName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets stage name text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Determines whether this stage is used in a transition.
    /// </summary>
    /// <param name="transition">Transition event.</param>
    /// <returns><see langword="true"/> when stage matches transition source or destination.</returns>
    public bool IsUsedInTransition(TransitionEvent transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return transition.From.Value.Contains(Value, StringComparison.OrdinalIgnoreCase)
            || transition.To.Value.Contains(Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns stage name text.
    /// </summary>
    /// <returns>Stage name text.</returns>
    public override string ToString() => Value;
}
