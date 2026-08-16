using CoreApp.Entities.Inventory;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Repositories;

public sealed class ItemInventoryRepository : IItemInventoryRepository
{
    private readonly IRepository<DbItemInventory> inventories;
    private readonly IRepository<DbItemContainerRelation> relations;
    private readonly IRepository<DbContainer> containers;
    private readonly ITransactionRunner transactionRunner;

    public ItemInventoryRepository(
        IRepository<DbItemInventory> inventories,
        IRepository<DbItemContainerRelation> relations,
        IRepository<DbContainer> containers,
        ITransactionRunner transactionRunner)
    {
        this.inventories = inventories;
        this.relations = relations;
        this.containers = containers;
        this.transactionRunner = transactionRunner;
    }

    /// <inheritdoc />
    public async Task<ItemInventory?> GetAsync(Guid itemId)
    {
        var inventoryRow = (await inventories.WhereAsync(inventory => inventory.ItemId == itemId).ConfigureAwait(false))
            .FirstOrDefault();
        if (inventoryRow is null)
        {
            return null;
        }

        var allocations = await LoadAllocationsAsync(itemId).ConfigureAwait(false);
        return new ItemInventory(itemId, inventoryRow.TotalQuantity, allocations);
    }

    /// <inheritdoc />
    public Task InsertAsync(ItemInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return SaveAsync(inventory);
    }

    /// <inheritdoc />
    public Task SaveAsync(ItemInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        return transactionRunner.RunAsync(scope =>
        {
            scope.InsertOrReplaceItemInventory(new DbItemInventory
            {
                ItemId = inventory.ItemId,
                TotalQuantity = inventory.TotalQuantity,
            });
            scope.DeleteRelationsByItem(inventory.ItemId);
            foreach (var allocation in inventory.Allocations.Where(allocation => allocation.Quantity > 0))
            {
                scope.InsertItemContainerRelation(
                    inventory.ItemId,
                    allocation.ContainerId,
                    allocation.Quantity);
            }
        });
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid itemId)
        => transactionRunner.RunAsync(scope =>
        {
            scope.DeleteRelationsByItem(itemId);
            scope.DeleteItemInventory(itemId);
        });

    private async Task<IReadOnlyList<ItemContainerAllocation>> LoadAllocationsAsync(Guid itemId)
    {
        var relationRows = (await relations.WhereAsync(
                relation => relation.ItemId == itemId && relation.Quantity > 0).ConfigureAwait(false))
            .GroupBy(relation => relation.ContainerId)
            .Select(group => new { ContainerId = group.Key, Quantity = group.Sum(row => row.Quantity) })
            .ToList();

        var result = new List<ItemContainerAllocation>(relationRows.Count);
        foreach (var relation in relationRows)
        {
            var container = await containers.GetAsync(relation.ContainerId.ToString()).ConfigureAwait(false);
            if (container is not null)
            {
                result.Add(new ItemContainerAllocation(relation.ContainerId, container.Name, relation.Quantity));
            }
        }

        return result
            .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
