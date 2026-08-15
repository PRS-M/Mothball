using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using CoreApp.Entities.ItemAggregate;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Simple repository for managing item-container relations.
/// </summary>
public interface IRelationRepository
{
    Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity);
    Task ReplaceItemContainerRelationQuantityAsync(Guid itemId, Guid containerId, int quantity);
    Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity);
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

    public async Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        await itemContainerRelations.InsertAsync(new DbItemContainerRelation
        {
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = quantity,
        });
    }

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
