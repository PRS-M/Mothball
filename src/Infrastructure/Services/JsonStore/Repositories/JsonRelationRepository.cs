using System;
using System.Threading.Tasks;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.Repositories;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonRelationRepository : IRelationRepository
{
    private readonly JsonInventoryStore store;

    public JsonRelationRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    public Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return store.UpdateAsync(state =>
        {
            state.Relations.Add(new JsonRelationRow
            {
                Id = state.Metadata.NextRelationId++,
                ItemId = itemId,
                ContainerId = containerId,
                Quantity = quantity,
            });

            return Task.CompletedTask;
        });
    }
}
