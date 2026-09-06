using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Application.Contracts.Tags;

namespace MothballMobile.UI.Features.Containers.ContainerDetails;

internal sealed class ContainerItemPagingController
{
    private readonly IContainerDetailsQueryHandler containerDetailsQueries;
    private const int PageSize = 5;
    private int currentPage;
    private bool hasMoreItems = true;
    private int loadVersion;
    private string? activeSearchTerm;
    private TagFilter? activeTagFilter;

    public ContainerItemPagingController(IContainerDetailsQueryHandler containerDetailsQueries)
    {
        this.containerDetailsQueries = containerDetailsQueries ?? throw new ArgumentNullException(nameof(containerDetailsQueries));
    }

    public void Reset()
    {
        currentPage = 0;
        hasMoreItems = true;
        activeSearchTerm = null;
        activeTagFilter = null;
        loadVersion++;
    }

    public void MarkComplete()
    {
        hasMoreItems = false;
    }

    public async Task<ContainerItemPageLoad> ReloadAsync(string containerId, string? searchTerm, TagFilter? tagFilter = null)
    {
        var version = ++loadVersion;
        currentPage = 0;
        hasMoreItems = false;
        activeSearchTerm = searchTerm;
        activeTagFilter = tagFilter;

        var items = await QueryAsync(containerId, currentPage, searchTerm, tagFilter);
        if (version != loadVersion)
        {
            return new ContainerItemPageLoad([], IsStale: true);
        }

        hasMoreItems = items.Count == PageSize;
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
        var items = await QueryAsync(containerId, pageToLoad, activeSearchTerm, activeTagFilter);

        if (version != loadVersion)
        {
            return new ContainerItemPageLoad([], IsStale: true);
        }

        currentPage = pageToLoad;
        hasMoreItems = items.Count == PageSize;
        return new ContainerItemPageLoad(items, IsStale: false);
    }

    private Task<List<ContainerItemInventoryEntry>> QueryAsync(
        string containerId,
        int pageNumber,
        string? searchTerm,
        TagFilter? tagFilter = null)
        => containerDetailsQueries.QueryItemsAsync(containerId, searchTerm, pageNumber, PageSize, tagFilter);
}
