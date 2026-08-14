using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed class ContainerItemPagingController
{
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly int pageSize;
    private int currentPage;
    private bool hasMoreItems = true;
    private int loadVersion;
    private string? activeSearchTerm;

    public ContainerItemPagingController(IInventoryQueryRepository inventoryQueries, int pageSize)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.pageSize = pageSize;
    }

    public void Reset()
    {
        currentPage = 0;
        hasMoreItems = true;
        activeSearchTerm = null;
        loadVersion++;
    }

    public void MarkComplete()
    {
        hasMoreItems = false;
    }

    public async Task<ContainerItemPageLoad> ReloadAsync(string containerId, string? searchTerm)
    {
        var version = ++loadVersion;
        currentPage = 0;
        hasMoreItems = false;
        activeSearchTerm = searchTerm;

        var items = await QueryAsync(containerId, currentPage, searchTerm);
        if (version != loadVersion)
        {
            return new ContainerItemPageLoad([], IsStale: true);
        }

        hasMoreItems = items.Count == pageSize;
        return new ContainerItemPageLoad(items, IsStale: false);
    }

    public async Task<ContainerItemPageLoad> LoadMoreAsync(string containerId)
    {
        if (!hasMoreItems || string.IsNullOrWhiteSpace(containerId))
        {
            return new ContainerItemPageLoad([], IsStale: false);
        }

        var version = loadVersion;
        var pageToLoad = currentPage + 1;
        var items = await QueryAsync(containerId, pageToLoad, activeSearchTerm);

        if (version != loadVersion)
        {
            return new ContainerItemPageLoad([], IsStale: true);
        }

        currentPage = pageToLoad;
        hasMoreItems = items.Count == pageSize;
        return new ContainerItemPageLoad(items, IsStale: false);
    }

    private Task<List<Item>> QueryAsync(string containerId, int pageNumber, string? searchTerm)
        => inventoryQueries.QueryContainerItemsWithPhotosAsync(
            new ContainerItemsSpecification(
                ContainerId: containerId,
                SearchTerm: searchTerm,
                PageNumber: pageNumber,
                PageSize: pageSize));
}
