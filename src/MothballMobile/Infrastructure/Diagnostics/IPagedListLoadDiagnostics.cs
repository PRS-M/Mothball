namespace MothballMobile.Infrastructure.Diagnostics;

public sealed record PagedListLoadMeasurement(
    string ListName,
    string Variant,
    int PageNumber,
    int PageSize,
    int ResultCount,
    double QueryElapsedMilliseconds,
    double PopulationElapsedMilliseconds,
    double TotalElapsedMilliseconds);

public interface IPagedListLoadDiagnostics
{
    void PageLoaded(PagedListLoadMeasurement measurement);
}
