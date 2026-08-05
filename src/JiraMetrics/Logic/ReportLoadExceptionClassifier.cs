using System.Text.Json;

using JiraMetrics.API;

namespace JiraMetrics.Logic;

/// <summary>
/// Classifies failures caused by external report-data loading.
/// </summary>
internal static class ReportLoadExceptionClassifier
{
    public static bool IsExpected(Exception exception) =>
        exception is HttpRequestException or JiraDataException or JsonException;
}
