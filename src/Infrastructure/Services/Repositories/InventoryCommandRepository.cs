using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.Entities.Shared;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Command-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryCommandRepository : IInventoryCommandRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IItemInventoryRepository itemInventoryRepo;
    private readonly IImageRepository imageRepo;
    private readonly IRelationRepository relationRepo;
    private readonly IInventoryChangeTracker? inventoryChanges;

    public InventoryCommandRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo,
        IImageRepository imageRepo,
        IRelationRepository relationRepo,
        IInventoryChangeTracker? inventoryChanges = null)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
        this.imageRepo = imageRepo;
        this.relationRepo = relationRepo;
        this.inventoryChanges = inventoryChanges;
    }

    /// <inheritdoc />
    public Task InsertContainerAsync(Container container)
        => TrackAsync(() => containerRepo.InsertAsync(container));

    /// <inheritdoc />
    public Task InsertItemAsync(Item item)
        => TrackAsync(() => itemRepo.InsertAsync(item));

    /// <inheritdoc />
    public Task InsertItemInventoryAsync(ItemInventory inventory)
        => TrackAsync(() => itemInventoryRepo.InsertAsync(inventory));

    /// <inheritdoc />
    public Task SaveItemInventoryAsync(ItemInventory inventory)
        => TrackAsync(() => itemInventoryRepo.SaveAsync(inventory));

    /// <inheritdoc />
    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => TrackAsync(() => imageRepo.InsertAsync(imageItem, ownerId));

    /// <inheritdoc />
    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        int existingQuantity = inventory.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
        inventory.SetContainerAllocation(containerId, string.Empty, existingQuantity + quantity);
        await itemInventoryRepo.SaveAsync(inventory);
        inventoryChanges?.MarkChanged();
    }

    /// <inheritdoc />
    public async Task ReplaceItemContainerRelationQuantity(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        inventory.SetContainerAllocation(containerId, string.Empty, quantity);
        await itemInventoryRepo.SaveAsync(inventory);
        inventoryChanges?.MarkChanged();
    }

    /// <inheritdoc />
    public Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity)
        => TrackAsync(() => relationRepo.SetItemContainerAllocationAsync(item, containerId, quantity));

    /// <inheritdoc />
    public Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation> allocations)
        => TrackAsync(() => relationRepo.ApplyItemInventoryWithdrawalAsync(item, allocations));

    /// <inheritdoc />
    public async Task DeleteItemContainerRelation(Guid itemId, Guid containerId)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId);
        if (inventory is null)
        {
            await relationRepo.DeleteItemContainerRelationAsync(itemId, containerId);
            inventoryChanges?.MarkChanged();
            return;
        }

        inventory.SetContainerAllocation(containerId, string.Empty, 0);
        await itemInventoryRepo.SaveAsync(inventory);
        inventoryChanges?.MarkChanged();
    }

    /// <inheritdoc />
    public Task UpdateContainerAsync(Container container)
        => TrackAsync(() => containerRepo.UpdateAsync(container));

    /// <inheritdoc />
    public Task UpdateItemAsync(Item item)
        => TrackAsync(() => itemRepo.UpdateAsync(item));

    /// <inheritdoc />
    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => TrackAsync(() => imageRepo.UpdateAsync(image, ownerId));

    /// <inheritdoc />
    public Task DeleteImageItemAsync(Guid imageId, Guid ownerId)
        => TrackAsync(() => imageRepo.DeleteAsync(imageId, ownerId));

    /// <inheritdoc />
    public Task DeleteContainerPhotoAsync(Container container, Guid imageId)
        => TrackAsync(() => containerRepo.DeletePhotoAsync(container, imageId));

    /// <inheritdoc />
    public Task DeleteItemPhotoAsync(Item item, Guid imageId)
        => TrackAsync(() => itemRepo.DeletePhotoAsync(item, imageId));

    /// <inheritdoc />
    public Task DeleteItemAsync(string itemId)
        => TrackAsync(() => itemRepo.DeleteAsync(itemId));

    /// <inheritdoc />
    public Task DeleteContainerAsync(string containerId)
        => TrackAsync(() => containerRepo.DeleteAsync(containerId));

    private async Task TrackAsync(Func<Task> mutation)
    {
        await mutation().ConfigureAwait(false);
        inventoryChanges?.MarkChanged();
    }
}
