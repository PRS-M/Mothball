using CoreApp.Domain.Entities.InventoryAggregate;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonItemInventoryRepository : IItemInventoryRepository
{
    private readonly JsonInventoryStore store;

    public JsonItemInventoryRepository(JsonInventoryStore store)
    {
        this.store = store;
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

        var distinctItemIds = itemIds
            .Where(itemId => itemId != Guid.Empty)
            .ToHashSet();
        if (distinctItemIds.Count == 0)
        {
            return new Dictionary<Guid, ItemInventory>();
        }

        var state = await store.LoadAsync().ConfigureAwait(false);
        var containersById = state.Containers.ToDictionary(container => container.ContainerId);
        var allocationsByItem = state.Relations
            .Where(relation => distinctItemIds.Contains(relation.ItemId)
                && relation.Quantity > 0
                && containersById.ContainsKey(relation.ContainerId))
            .GroupBy(relation => new { relation.ItemId, relation.ContainerId })
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

        return state.Inventories
            .Where(inventory => distinctItemIds.Contains(inventory.ItemId))
            .ToDictionary(
                inventory => inventory.ItemId,
                inventory => new ItemInventory(
                    inventory.ItemId,
                    inventory.TotalQuantity,
                    allocationsByItem.GetValueOrDefault(inventory.ItemId) ?? []));
    }

    /// <inheritdoc />
    public Task InsertAsync(ItemInventory inventory)
        => SaveAsync(inventory);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task DeleteAsync(Guid itemId)
        => store.UpdateAsync(state =>
        {
            state.Relations.RemoveAll(relation => relation.ItemId == itemId);
            state.Inventories.RemoveAll(inventory => inventory.ItemId == itemId);
            return Task.CompletedTask;
        });

}
