using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.Repositories;
using CoreApp.Domain.Entities.ItemAggregate;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonRelationRepository : IRelationRepository
{
    private readonly JsonInventoryStore store;

    public JsonRelationRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    /// <inheritdoc />
    public Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return store.UpdateAsync(state =>
        {
            InsertOrIncreaseRelation(state, itemId, containerId, quantity);

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task ReplaceItemContainerRelationQuantityAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return store.UpdateAsync(state =>
        {
            state.Relations.RemoveAll(r => r.ItemId == itemId && r.ContainerId == containerId);

            if (quantity > 0)
            {
                state.Relations.Add(new JsonRelationRow
                {
                    Id = state.Metadata.NextRelationId++,
                    ItemId = itemId,
                    ContainerId = containerId,
                    Quantity = quantity,
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return store.UpdateAsync(state =>
        {
            var itemRow = state.Items.FirstOrDefault(row => row.ItemId == item.ItemId)
                ?? throw new KeyNotFoundException($"Item '{item.ItemId}' was not found.");
            itemRow.Name = item.Name;
            itemRow.Description = item.Description;

            state.Relations.RemoveAll(relation =>
                relation.ItemId == item.ItemId && relation.ContainerId == containerId);

            if (quantity > 0)
            {
                state.Relations.Add(new JsonRelationRow
                {
                    Id = state.Metadata.NextRelationId++,
                    ItemId = item.ItemId,
                    ContainerId = containerId,
                    Quantity = quantity,
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(allocations);

        return store.UpdateAsync(state =>
        {
            var itemRow = state.Items.FirstOrDefault(row => row.ItemId == item.ItemId)
                ?? throw new KeyNotFoundException($"Item '{item.ItemId}' was not found.");
            itemRow.Name = item.Name;
            itemRow.Description = item.Description;

            state.Relations.RemoveAll(relation => relation.ItemId == item.ItemId);
            foreach (var allocation in allocations.Where(allocation => allocation.Quantity > 0))
            {
                state.Relations.Add(new JsonRelationRow
                {
                    Id = state.Metadata.NextRelationId++,
                    ItemId = item.ItemId,
                    ContainerId = allocation.ContainerId,
                    Quantity = allocation.Quantity,
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task DeleteItemContainerRelationAsync(Guid itemId, Guid containerId)
    {
        return store.UpdateAsync(state =>
        {
            state.Relations.RemoveAll(r => r.ItemId == itemId && r.ContainerId == containerId);
            return Task.CompletedTask;
        });
    }

    private static void InsertOrIncreaseRelation(
        JsonInventoryStore.StoreState state,
        Guid itemId,
        Guid containerId,
        int quantity)
    {
        var existingQuantity = state.Relations
            .Where(relation => relation.ItemId == itemId && relation.ContainerId == containerId)
            .Sum(relation => relation.Quantity);

        state.Relations.RemoveAll(relation => relation.ItemId == itemId && relation.ContainerId == containerId);
        state.Relations.Add(new JsonRelationRow
        {
            Id = state.Metadata.NextRelationId++,
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = existingQuantity + quantity,
        });
    }
}
