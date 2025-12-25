using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Simple repository for managing item-container relations.
/// </summary>
public interface IRelationRepository
{
    Task InsertItemContainerRelationAsync(Guid itemId, Guid containerId, int quantity);
}

public class RelationRepository : IRelationRepository
{
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;

    public RelationRepository(IRepository<DbItemContainerRelation> itemContainerRelations)
    {
        this.itemContainerRelations = itemContainerRelations;
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
}
