namespace Infrastructure.Abstractions.Transactions;

using Infrastructure.Services.DatabaseModels;

public interface ITransactionRunner
{
    Task RunAsync(Action<ITransactionalDeleteScope> action);
}

public interface ITransactionalDeleteScope
{
    void DeleteImage(Guid imageId, Guid ownerId);
    void DeleteImagesByOwner(Guid ownerId);
    void DeleteRelationsByContainer(Guid containerId);
    void DeleteRelationsByItem(Guid itemId);
    void UpdateContainer(DbContainer container);
    void UpdateItem(DbItem item);
    void InsertOrReplaceItemInventory(DbItemInventory inventory);
    void DeleteItemInventory(Guid itemId);
    void ReplaceItemContainerRelation(Guid itemId, Guid containerId, int quantity);
    void InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);
    void DeleteContainer(Guid containerId);
    void DeleteItem(Guid itemId);
}
