namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a human-readable transition path label.
/// </summary>
public readonly record struct PathLabel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathLabel"/> struct.
    /// </summary>
    /// <param name="value">Path label text.</param>
    public PathLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    /// <summary>
    /// Gets path label text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Builds path label from transitions.
    /// </summary>
    /// <param name="transitions">Transition events.</param>
    /// <returns>Path label.</returns>
    public static PathLabel FromTransitions(IReadOnlyList<TransitionEvent> transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions);

        if (transitions.Count == 0)
        {
            return new PathLabel("No transitions");
        }

        return new PathLabel(string.Join(" | ", transitions.Select(static x => $"{x.From} -> {x.To}")));
    }

    /// <summary>
    /// Returns path label text.
    /// </summary>
    /// <returns>Path label text.</returns>
    public override string ToString() => Value;
}
