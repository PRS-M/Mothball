using CoreApp.Entities.Inventory;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using CoreApp.Entities.ItemAggregate;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Simple repository for managing item-container relations.
/// </summary>
public interface IRelationRepository
{
    /// <summary>
    /// Creates an item-to-container allocation with the specified quantity.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the receiving container.</param>
    /// <param name="quantity">The quantity to allocate.</param>
    Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity);
    /// <summary>
    /// Replaces the quantity assigned to an item in a container.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the container.</param>
    /// <param name="quantity">The replacement allocation quantity.</param>
    Task ReplaceItemContainerRelationQuantityAsync(Guid itemId, Guid containerId, int quantity);
    /// <summary>
    /// Updates an item and sets its allocation in a container.
    /// </summary>
    /// <param name="item">The item with updated allocation state.</param>
    /// <param name="containerId">The identifier of the container.</param>
    /// <param name="quantity">The allocation quantity.</param>
    Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity);
    /// <summary>
    /// Persists an item's allocations after an inventory withdrawal.
    /// </summary>
    /// <param name="item">The item after the withdrawal.</param>
    /// <param name="allocations">The remaining allocations to persist.</param>
    Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Entities.Inventory.ItemContainerAllocation> allocations);
    /// <summary>
    /// Removes the allocation between an item and a container.
    /// </summary>
    /// <param name="itemId">The identifier of the allocated item.</param>
    /// <param name="containerId">The identifier of the container.</param>
    Task DeleteItemContainerRelationAsync(Guid itemId, Guid containerId);
}

public class RelationRepository : IRelationRepository
{
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly ITransactionRunner transactionRunner;

    public RelationRepository(
        IRepository<DbItemContainerRelation> itemContainerRelations,
        ITransactionRunner transactionRunner)
    {
        this.itemContainerRelations = itemContainerRelations;
        this.transactionRunner = transactionRunner;
    }

    /// <inheritdoc />
    public async Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        await transactionRunner.RunAsync(scope =>
        {
            scope.InsertItemContainerRelation(itemId, containerId, quantity);
        });
    }

    /// <inheritdoc />
    public async Task ReplaceItemContainerRelationQuantityAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        var existing = await itemContainerRelations
            .WhereAsync(r => r.ItemId == itemId && r.ContainerId == containerId)
            .ConfigureAwait(false);

        foreach (var row in existing)
        {
            await itemContainerRelations.DeleteAsync(row).ConfigureAwait(false);
        }

        if (quantity == 0) return;

        await itemContainerRelations.InsertAsync(new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = quantity,
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return transactionRunner.RunAsync(scope =>
        {
            scope.UpdateItem(item.ToDb());
            scope.ReplaceItemContainerRelation(item.ItemId, containerId, quantity);
        });
    }

    /// <inheritdoc />
    public Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Entities.Inventory.ItemContainerAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(allocations);

        return transactionRunner.RunAsync(scope =>
        {
            scope.UpdateItem(item.ToDb());
            scope.DeleteRelationsByItem(item.ItemId);
            foreach (var allocation in allocations.Where(allocation => allocation.Quantity > 0))
            {
                scope.InsertItemContainerRelation(
                    item.ItemId,
                    allocation.ContainerId,
                    allocation.Quantity);
            }
        });
    }

    /// <inheritdoc />
    public async Task DeleteItemContainerRelationAsync(Guid itemId, Guid containerId)
    {
        var existing = await itemContainerRelations
            .WhereAsync(r => r.ItemId == itemId && r.ContainerId == containerId)
            .ConfigureAwait(false);

        foreach (var row in existing)
        {
            await itemContainerRelations.DeleteAsync(row).ConfigureAwait(false);
        }
    }
}
