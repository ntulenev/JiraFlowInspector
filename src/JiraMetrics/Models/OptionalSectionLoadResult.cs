namespace JiraMetrics.Models;

/// <summary>
/// Represents the result of loading one enabled optional report section.
/// </summary>
internal abstract record OptionalSectionLoadResult<T>
{
    private OptionalSectionLoadResult()
    {
    }

    internal sealed record Loaded(T Value) : OptionalSectionLoadResult<T>;

    internal sealed record Failed(OptionalSectionLoadFailure Failure) :
        OptionalSectionLoadResult<T>;
}
