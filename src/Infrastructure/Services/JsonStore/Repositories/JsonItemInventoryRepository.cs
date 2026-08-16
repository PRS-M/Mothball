using CoreApp.Entities.Inventory;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonItemInventoryRepository : IItemInventoryRepository
{
    private readonly JsonInventoryStore store;

    public JsonItemInventoryRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    public async Task<ItemInventory?> GetAsync(Guid itemId)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var inventoryRow = state.Inventories.FirstOrDefault(row => row.ItemId == itemId);
        if (inventoryRow is null)
        {
            return null;
        }

        return new ItemInventory(itemId, inventoryRow.TotalQuantity, LoadAllocations(state, itemId));
    }

    public Task InsertAsync(ItemInventory inventory)
        => SaveAsync(inventory);

    public Task SaveAsync(ItemInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        return store.UpdateAsync(state =>
        {
            var existing = state.Inventories.FirstOrDefault(row => row.ItemId == inventory.ItemId);
            if (existing is null)
            {
                state.Inventories.Add(new JsonInventoryRow
                {
                    ItemId = inventory.ItemId,
                    TotalQuantity = inventory.TotalQuantity,
                });
            }
            else
            {
                existing.TotalQuantity = inventory.TotalQuantity;
            }

            state.Relations.RemoveAll(relation => relation.ItemId == inventory.ItemId);
            foreach (var allocation in inventory.Allocations.Where(allocation => allocation.Quantity > 0))
            {
                state.Relations.Add(new JsonRelationRow
                {
                    Id = state.Metadata.NextRelationId++,
                    ItemId = inventory.ItemId,
                    ContainerId = allocation.ContainerId,
                    Quantity = allocation.Quantity,
                });
            }

            return Task.CompletedTask;
        });
    }

    public Task DeleteAsync(Guid itemId)
        => store.UpdateAsync(state =>
        {
            state.Relations.RemoveAll(relation => relation.ItemId == itemId);
            state.Inventories.RemoveAll(inventory => inventory.ItemId == itemId);
            return Task.CompletedTask;
        });

    private static IReadOnlyList<ItemContainerAllocation> LoadAllocations(JsonInventoryStore.StoreState state, Guid itemId)
        => state.Relations
            .Where(relation => relation.ItemId == itemId && relation.Quantity > 0)
            .GroupBy(relation => relation.ContainerId)
            .Select(group =>
            {
                var container = state.Containers.FirstOrDefault(row => row.ContainerId == group.Key);
                return container is null
                    ? null
                    : new ItemContainerAllocation(group.Key, container.Name, group.Sum(relation => relation.Quantity));
            })
            .Where(allocation => allocation is not null)
            .Select(allocation => allocation!)
            .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
