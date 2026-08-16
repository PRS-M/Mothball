using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;

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

    public InventoryCommandRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo,
        IImageRepository imageRepo,
        IRelationRepository relationRepo)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
        this.imageRepo = imageRepo;
        this.relationRepo = relationRepo;
    }

    /// <inheritdoc />
    public Task InsertContainerAsync(Container container)
        => containerRepo.InsertAsync(container);

    /// <inheritdoc />
    public Task InsertItemAsync(Item item)
        => itemRepo.InsertAsync(item);

    /// <inheritdoc />
    public Task InsertItemInventoryAsync(ItemInventory inventory)
        => itemInventoryRepo.InsertAsync(inventory);

    /// <inheritdoc />
    public Task SaveItemInventoryAsync(ItemInventory inventory)
        => itemInventoryRepo.SaveAsync(inventory);

    /// <inheritdoc />
    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => imageRepo.InsertAsync(imageItem, ownerId);

    /// <inheritdoc />
    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        int existingQuantity = inventory.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
        inventory.SetContainerAllocation(containerId, string.Empty, existingQuantity + quantity);
        await itemInventoryRepo.SaveAsync(inventory);
    }

    /// <inheritdoc />
    public async Task ReplaceItemContainerRelationQuantity(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        inventory.SetContainerAllocation(containerId, string.Empty, quantity);
        await itemInventoryRepo.SaveAsync(inventory);
    }

    /// <inheritdoc />
    public Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity)
        => relationRepo.SetItemContainerAllocationAsync(item, containerId, quantity);

    /// <inheritdoc />
    public Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Entities.Inventory.ItemContainerAllocation> allocations)
        => relationRepo.ApplyItemInventoryWithdrawalAsync(item, allocations);

    /// <inheritdoc />
    public async Task DeleteItemContainerRelation(Guid itemId, Guid containerId)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId);
        if (inventory is null)
        {
            await relationRepo.DeleteItemContainerRelationAsync(itemId, containerId);
            return;
        }

        inventory.SetContainerAllocation(containerId, string.Empty, 0);
        await itemInventoryRepo.SaveAsync(inventory);
    }

    /// <inheritdoc />
    public Task UpdateContainerAsync(Container container)
        => containerRepo.UpdateAsync(container);

    /// <inheritdoc />
    public Task UpdateItemAsync(Item item)
        => itemRepo.UpdateAsync(item);

    /// <inheritdoc />
    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => imageRepo.UpdateAsync(image, ownerId);

    /// <inheritdoc />
    public Task DeleteImageItemAsync(Guid imageId, Guid ownerId)
        => imageRepo.DeleteAsync(imageId, ownerId);

    /// <inheritdoc />
    public Task DeleteContainerPhotoAsync(Container container, Guid imageId)
        => containerRepo.DeletePhotoAsync(container, imageId);

    /// <inheritdoc />
    public Task DeleteItemPhotoAsync(Item item, Guid imageId)
        => itemRepo.DeletePhotoAsync(item, imageId);

    /// <inheritdoc />
    public Task DeleteItemAsync(string itemId)
        => itemRepo.DeleteAsync(itemId);

    /// <inheritdoc />
    public Task DeleteContainerAsync(string containerId)
        => containerRepo.DeleteAsync(containerId);
}
