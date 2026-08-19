using CoreApp.Entities.InventoryAggregate;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Contracts;

namespace CoreApp.Abstractions.Persistence;

/// <summary>
/// Defines commands that persist inventory changes.
/// </summary>
public interface IInventoryCommandRepository
{
    /// <summary>
    /// Persists a new container.
    /// </summary>
    /// <param name="container">The container to persist.</param>
    Task InsertContainerAsync(Container container);

    /// <summary>
    /// Persists a new item.
    /// </summary>
    /// <param name="item">The item to persist.</param>
    Task InsertItemAsync(Item item);

    /// <summary>
    /// Persists the initial inventory record for an item.
    /// </summary>
    /// <param name="inventory">The inventory record to persist.</param>
    Task InsertItemInventoryAsync(ItemInventory inventory);

    /// <summary>
    /// Persists changes to an item's inventory record.
    /// </summary>
    /// <param name="inventory">The inventory record with updated values.</param>
    Task SaveItemInventoryAsync(ItemInventory inventory);

    /// <summary>
    /// Persists an image and associates it with its owner.
    /// </summary>
    /// <param name="imageItem">The image metadata to persist.</param>
    /// <param name="ownerId">The identifier of the image owner.</param>
    Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId);

    /// <summary>
    /// Creates an item-to-container allocation with the specified quantity.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the receiving container.</param>
    /// <param name="quantity">The quantity to allocate.</param>
    Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);

    /// <summary>
    /// Replaces the quantity assigned to an item in a container.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the container.</param>
    /// <param name="quantity">The replacement allocation quantity.</param>
    Task ReplaceItemContainerRelationQuantity(Guid itemId, Guid containerId, int quantity);

    /// <summary>
    /// Updates an item and sets its allocation in a container.
    /// </summary>
    /// <param name="item">The item with updated allocation state.</param>
    /// <param name="containerId">The identifier of the container.</param>
    /// <param name="quantity">The allocation quantity.</param>
    Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity);

    /// <summary>
    /// Persists an item's updated allocations after an inventory withdrawal.
    /// </summary>
    /// <param name="item">The item after the withdrawal.</param>
    /// <param name="allocations">The remaining allocations to persist.</param>
    Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<ItemContainerAllocation> allocations);

    /// <summary>
    /// Removes the allocation between an item and a container.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the container.</param>
    Task DeleteItemContainerRelation(Guid itemId, Guid containerId);

    /// <summary>
    /// Persists changes to a container.
    /// </summary>
    /// <param name="container">The container with updated values.</param>
    Task UpdateContainerAsync(Container container);

    /// <summary>
    /// Persists changes to an item.
    /// </summary>
    /// <param name="item">The item with updated values.</param>
    Task UpdateItemAsync(Item item);

    /// <summary>
    /// Persists changes to an image associated with an owner.
    /// </summary>
    /// <param name="image">The image metadata with updated values.</param>
    /// <param name="ownerId">The identifier of the image owner.</param>
    Task UpdateImageItemAsync(ImageItem image, Guid ownerId);

    /// <summary>
    /// Deletes an image associated with an owner.
    /// </summary>
    /// <param name="imageId">The identifier of the image to delete.</param>
    /// <param name="ownerId">The identifier of the image owner.</param>
    Task DeleteImageItemAsync(Guid imageId, Guid ownerId);

    /// <summary>
    /// Deletes a photo from a container.
    /// </summary>
    /// <param name="container">The container that owns the photo.</param>
    /// <param name="imageId">The identifier of the photo to delete.</param>
    Task DeleteContainerPhotoAsync(Container container, Guid imageId);

    /// <summary>
    /// Deletes a photo from an item.
    /// </summary>
    /// <param name="item">The item that owns the photo.</param>
    /// <param name="imageId">The identifier of the photo to delete.</param>
    Task DeleteItemPhotoAsync(Item item, Guid imageId);

    /// <summary>
    /// Deletes an item identified by its string identifier.
    /// </summary>
    /// <param name="itemId">The string identifier of the item to delete.</param>
    Task DeleteItemAsync(string itemId);

    /// <summary>
    /// Deletes a container identified by its string identifier.
    /// </summary>
    /// <param name="containerId">The string identifier of the container to delete.</param>
    Task DeleteContainerAsync(string containerId);
}
