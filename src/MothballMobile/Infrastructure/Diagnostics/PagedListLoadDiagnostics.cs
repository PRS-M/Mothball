using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure.Diagnostics;

public sealed class PagedListLoadDiagnostics : IPagedListLoadDiagnostics
{
    private readonly ILogger<PagedListLoadDiagnostics> logger;

    public PagedListLoadDiagnostics(ILogger<PagedListLoadDiagnostics> logger)
    {
        this.logger = logger;
    }

    public void PageLoaded(PagedListLoadMeasurement measurement)
    {
        logger.LogInformation(
            "Paged list loaded: list={ListName}, variant={Variant}, page={PageNumber}, pageSize={PageSize}, results={ResultCount}, queryMs={QueryElapsedMs:F1}, populationMs={PopulationElapsedMs:F1}, totalMs={TotalElapsedMs:F1}",
            measurement.ListName,
            measurement.Variant,
            measurement.PageNumber,
            measurement.PageSize,
            measurement.ResultCount,
            measurement.QueryElapsedMilliseconds,
            measurement.PopulationElapsedMilliseconds,
            measurement.TotalElapsedMilliseconds);
    }
}
