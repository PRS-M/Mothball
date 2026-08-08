using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Thin facade that composes focused repositories for a cohesive domain-oriented API.
/// </summary>
public class InventoryDomainRepository : IInventoryDomainRepository
{
    private readonly IInventoryQueryRepository queries;
    private readonly IInventoryCommandRepository commands;

    public InventoryDomainRepository(
        IInventoryQueryRepository queries,
        IInventoryCommandRepository commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    #region Container Operations

    /// <inheritdoc />
    public Task<Container?> GetContainerAsync(string containerId)
        => queries.GetContainerAsync(containerId);

    /// <inheritdoc />
    public Task<List<Container>> GetAllContainersAsync()
        => queries.GetAllContainersAsync();

    /// <inheritdoc />
    public Task<List<Container>> GetAllContainersAsync(int pageNumber, int pageSize)
        => queries.GetAllContainersAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId)
        => await queries.GetContainerWithItemsAndPhotosAsync(containerId);

    /// <inheritdoc />
    public async Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize)
        => await queries.GetContainerWithItemsAndPhotosAsync(containerId, pageNumber, pageSize);

    /// <inheritdoc />
    public Task<int> GetItemCountInContainerAsync(string containerId)
        => queries.GetItemCountInContainerAsync(containerId);

    /// <inheritdoc />
    public Task<Container?> GetContainerForItemAsync(string itemId)
        => queries.GetContainerForItemAsync(itemId);

    /// <inheritdoc />
    public Task InsertContainerAsync(Container container)
        => commands.InsertContainerAsync(container);

    /// <inheritdoc />
    public Task UpdateContainerAsync(Container container)
        => commands.UpdateContainerAsync(container);

    /// <inheritdoc />
    public Task DeleteContainerAsync(string containerId)
        => commands.DeleteContainerAsync(containerId);

    #endregion

    #region Item Operations

    /// <inheritdoc />
    public Task<List<Item>> GetItemsForContainerAsync(string containerId)
        => queries.GetItemsForContainerAsync(containerId);

    /// <inheritdoc />
    public Task<Item?> GetItemWithPhotosAsync(string itemId)
        => queries.GetItemWithPhotosAsync(itemId);

    /// <inheritdoc />
    public Task<List<Item>> GetAllItemsWithPhotosAsync()
        => queries.GetAllItemsWithPhotosAsync();

    /// <inheritdoc />
    public Task<List<Item>> GetAllItemsWithPhotosAsync(int pageNumber, int pageSize)
        => queries.GetAllItemsWithPhotosAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public Task<List<Item>> GetUnassignedItemsWithPhotosAsync(int pageNumber, int pageSize)
        => queries.GetUnassignedItemsWithPhotosAsync(pageNumber, pageSize);

    /// <inheritdoc />
    public Task<List<Item>> GetItemsWithPhotosAsync(string searchTerm)
        => queries.GetItemsWithPhotosAsync(searchTerm);

    /// <inheritdoc />
    public Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
        => queries.SearchItemsInContainerAsync(containerId, searchTerm, pageNumber, pageSize);

    /// <inheritdoc />
    public Task InsertItemAsync(Item item)
        => commands.InsertItemAsync(item);

    /// <inheritdoc />
    public Task UpdateItemAsync(Item item)
        => commands.UpdateItemAsync(item);

    /// <inheritdoc />
    public Task DeleteItemAsync(string itemId)
        => commands.DeleteItemAsync(itemId);

    #endregion

    #region Image Operations

    /// <inheritdoc />
    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => commands.InsertImageItemAsync(imageItem, ownerId);

    /// <inheritdoc />
    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => commands.UpdateImageItemAsync(image, ownerId);

    #endregion

    #region Relation Operations

    /// <inheritdoc />
    public Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
        => commands.InsertItemContainerRelation(itemId, containerId, quantity);

    #endregion
}
