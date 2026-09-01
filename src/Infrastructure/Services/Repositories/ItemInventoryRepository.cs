using CoreApp.Domain.Entities.InventoryAggregate;
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
        var result = await GetManyAsync([itemId]).ConfigureAwait(false);
        return result.GetValueOrDefault(itemId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ItemInventory>> GetManyAsync(IReadOnlyCollection<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        var distinctItemIds = NormalizeItemIds(itemIds);
        if (distinctItemIds.Count == 0)
        {
            return new Dictionary<Guid, ItemInventory>();
        }

        var boxedItemIds = BoxIds(distinctItemIds);

        var inventoryRows = await inventories
            .WhereInAsync(nameof(DbItemInventory.ItemId), boxedItemIds)
            .ConfigureAwait(false);

        var relationRows = await LoadPositiveRelationsAsync(boxedItemIds);
        var allocationsByItem = await BuildAllocationsByItemAsync(relationRows);

        return MapInventories(inventoryRows, allocationsByItem);
    }

    private static List<Guid> NormalizeItemIds(IReadOnlyCollection<Guid> itemIds)
        => itemIds
            .Where(itemId => itemId != Guid.Empty)
            .Distinct()
            .ToList();

    private static List<object> BoxIds(IEnumerable<Guid> ids)
        => ids.Select(id => (object)id).ToList();

    private async Task<List<DbItemContainerRelation>> LoadPositiveRelationsAsync(
        IReadOnlyCollection<object> boxedItemIds)
        => (await relations
                .WhereInAsync(nameof(DbItemContainerRelation.ItemId), boxedItemIds)
                .ConfigureAwait(false))
            .Where(relation => relation.Quantity > 0)
            .ToList();

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> BuildAllocationsByItemAsync(
        IReadOnlyCollection<DbItemContainerRelation> relationRows)
    {
        var containersById = await LoadContainersByIdAsync(relationRows);

        return relationRows
            .GroupBy(relation => new { relation.ItemId, relation.ContainerId })
            .Where(group => containersById.ContainsKey(group.Key.ContainerId))
            .GroupBy(group => group.Key.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemContainerAllocation>)group
                    .Select(relationGroup => new ItemContainerAllocation(
                        relationGroup.Key.ContainerId,
                        containersById[relationGroup.Key.ContainerId].Name,
                        relationGroup.Sum(relation => relation.Quantity)))
                    .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    private async Task<IReadOnlyDictionary<Guid, DbContainer>> LoadContainersByIdAsync(
        IReadOnlyCollection<DbItemContainerRelation> relationRows)
    {
        var containerIds = relationRows
            .Select(relation => relation.ContainerId)
            .Distinct()
            .ToList();

        if (containerIds.Count == 0)
        {
            return new Dictionary<Guid, DbContainer>();
        }

        var rows = await containers
            .WhereInAsync(nameof(DbContainer.ContainerId), BoxIds(containerIds))
            .ConfigureAwait(false);

        return rows.ToDictionary(container => container.ContainerId);
    }

    private static IReadOnlyDictionary<Guid, ItemInventory> MapInventories(
        IEnumerable<DbItemInventory> inventoryRows,
        IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>> allocationsByItem)
        => inventoryRows.ToDictionary(
            inventory => inventory.ItemId,
            inventory => new ItemInventory(
                inventory.ItemId,
                inventory.TotalQuantity,
                allocationsByItem.GetValueOrDefault(inventory.ItemId) ?? []));

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

}
