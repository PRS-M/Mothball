using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Contracts;

namespace CoreApp.Interfaces;

public interface IInventoryCommandRepository
{
    Task InsertContainerAsync(Container container);

    Task InsertItemAsync(Item item);

    Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId);

    Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);

    Task ReplaceItemContainerRelationQuantity(Guid itemId, Guid containerId, int quantity);

    Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity);

    Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<ItemContainerAllocation> allocations);

    Task DeleteItemContainerRelation(Guid itemId, Guid containerId);

    Task UpdateContainerAsync(Container container);

    Task UpdateItemAsync(Item item);

    Task UpdateImageItemAsync(ImageItem image, Guid ownerId);

    Task DeleteImageItemAsync(Guid imageId, Guid ownerId);

    Task DeleteContainerPhotoAsync(Container container, Guid imageId);

    Task DeleteItemPhotoAsync(Item item, Guid imageId);

    Task DeleteItemAsync(string itemId);

    Task DeleteContainerAsync(string containerId);
}