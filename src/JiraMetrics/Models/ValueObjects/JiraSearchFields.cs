namespace JiraMetrics.Models.ValueObjects;

/// <summary>
/// Represents a normalized list of Jira search fields.
/// </summary>
public sealed class JiraSearchFields : IReadOnlyList<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraSearchFields"/> class.
    /// </summary>
    /// <param name="values">Field names or field ids.</param>
    public JiraSearchFields(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _values = [.. values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Gets an empty field selection.
    /// </summary>
    public static JiraSearchFields Empty { get; } = new([]);

    /// <summary>
    /// Creates a field selection from values.
    /// </summary>
    /// <param name="values">Field names or field ids.</param>
    /// <returns>Normalized field selection.</returns>
    public static JiraSearchFields From(params string[] values) => new(values);

    /// <summary>
    /// Gets field count.
    /// </summary>
    public int Count => _values.Length;

    /// <summary>
    /// Gets a field value by index.
    /// </summary>
    /// <param name="index">Field index.</param>
    /// <returns>Field value.</returns>
    public string this[int index] => _values[index];

    /// <summary>
    /// Returns the normalized field list.
    /// </summary>
    /// <returns>Field values.</returns>
    public IReadOnlyList<string> ToList() => _values;

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_values).GetEnumerator();

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private readonly string[] _values;
}
