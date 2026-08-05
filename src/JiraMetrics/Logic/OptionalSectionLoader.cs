using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Isolates expected failures from enabled optional report-section loads.
/// </summary>
internal static class OptionalSectionLoader
{
    public static async Task<OptionalSectionLoadResult<T>> LoadAsync<T>(
        OptionalReportSection section,
        Func<CancellationToken, Task<T>> load,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(load);

        try
        {
            return new OptionalSectionLoadResult<T>.Loaded(
                await load(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ReportLoadExceptionClassifier.IsExpected(ex))
        {
            return new OptionalSectionLoadResult<T>.Failed(
                new OptionalSectionLoadFailure(section, ErrorMessage.FromException(ex)));
        }
    }
}
